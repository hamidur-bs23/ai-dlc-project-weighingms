using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WeighingMS.Api.Common;
using WeighingMS.Api.Contracts;
using WeighingMS.Application.Abstractions;
using WeighingMS.Application.Access;

namespace WeighingMS.Api.Controllers;

/// <summary>Station session: badge sign-in, current session, sign-out and supervisor step-up (ADR-009).</summary>
[Route("api/session")]
public sealed class SessionController(StationAccessService access, IPlantClock clock, StationAccessOptions options, IAntiforgery antiforgery) : ApiControllerBase
{
    /// <summary>Signs in with a badge. A new sign-in replaces the current session (audited).</summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(StationPolicies.AuthRateLimit)]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var current = User.Identity?.IsAuthenticated == true ? StationClaims.BadgeOf(User) : null;
        var result = await access.SignInAsync(new SignInCommand(request.BadgeId ?? string.Empty, request.Shift, current), cancellationToken);
        if (result.IsError)
        {
            return Problem(result.Errors);
        }

        var session = result.Value;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new(ClaimTypes.Name, session.DisplayName),
            new(ClaimTypes.Role, session.Role.ToString()),
            new(StationClaims.Badge, session.BadgeId),
            new(StationClaims.SignedInAt, session.SignedInAt.ToString("O", CultureInfo.InvariantCulture)),
        };
        if (session.Shift is { } shift)
        {
            claims.Add(new Claim(StationClaims.Shift, shift));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        HttpContext.User = principal;
        AntiforgeryCookies.Issue(HttpContext, antiforgery);
        return Ok(ToResponse(session.BadgeId, session.DisplayName, session.Role.ToString(), session.Shift, session.SignedInAt));
    }

    /// <summary>The current session.</summary>
    [HttpGet]
    [Authorize(Policy = StationPolicies.RequireOperator)]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public SessionResponse Current() =>
        ToResponse(
            StationClaims.BadgeOf(User) ?? string.Empty,
            User.Identity?.Name ?? string.Empty,
            User.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
            User.FindFirstValue(StationClaims.Shift),
            DateTimeOffset.Parse(User.FindFirstValue(StationClaims.SignedInAt) ?? clock.UtcNow.ToString("O", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture));

    /// <summary>Signs out (audited).</summary>
    [HttpDelete]
    [Authorize(Policy = StationPolicies.RequireOperator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SignOut(CancellationToken cancellationToken)
    {
        await access.SignOutAsync(StationClaims.BadgeOf(User) ?? string.Empty, cancellationToken);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        AntiforgeryCookies.Issue(HttpContext, antiforgery);
        return NoContent();
    }

    /// <summary>Supervisor badge + PIN step-up for one action; returns a single-use grant.</summary>
    [HttpPost("step-up")]
    [Authorize(Policy = StationPolicies.RequireOperator)]
    [EnableRateLimiting(StationPolicies.AuthRateLimit)]
    [ProducesResponseType<StepUpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> StepUp([FromBody] StepUpRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var command = new StepUpCommand(StationClaims.BadgeOf(User) ?? string.Empty, request.BadgeId ?? string.Empty, request.Pin ?? string.Empty, request.Action ?? string.Empty);
        var result = await access.RequestStepUpAsync(command, cancellationToken);
        return result.Match<IActionResult>(
            g => Ok(new StepUpResponse(g.GrantId, g.SupervisorBadgeId, g.SupervisorName, g.Action, g.ExpiresAt)),
            Problem);
    }

    private SessionResponse ToResponse(string badge, string name, string role, string? shift, DateTimeOffset signedInAt) =>
        new(badge, name, role.ToLowerInvariant(), shift, options.StationId, clock.TimeZoneId, signedInAt);
}
