using Microsoft.AspNetCore.Antiforgery;

namespace WeighingMS.Api.Common;

/// <summary>
/// Issues the readable <c>XSRF-TOKEN</c> cookie (double-submit pattern: the SPA echoes it as <c>X-XSRF-TOKEN</c>).
/// Tokens are bound to the signed-in identity, so a new one is issued whenever the identity changes
/// (sign-in, sign-out).
/// </summary>
public static class AntiforgeryCookies
{
    /// <summary>Name of the readable cookie carrying the request token.</summary>
    public const string RequestTokenCookie = "XSRF-TOKEN";

    /// <summary>Generates a token pair for the current <see cref="HttpContext.User"/> and sets both cookies.</summary>
    public static void Issue(HttpContext context, IAntiforgery antiforgery)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(antiforgery);
        var tokens = antiforgery.GetAndStoreTokens(context);

        // Readable by the SPA on purpose; it is not a session secret (the session cookie is HttpOnly).
        // Secure whenever the request is HTTPS (always outside local Development).
#pragma warning disable S3330, S2092
        context.Response.Cookies.Append(RequestTokenCookie, tokens.RequestToken ?? string.Empty, new CookieOptions
        {
            HttpOnly = false,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
        });
#pragma warning restore S3330, S2092
    }
}
