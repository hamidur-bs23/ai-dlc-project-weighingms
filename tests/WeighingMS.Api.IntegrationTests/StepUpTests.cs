using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeighingMS.Application.Access;
using WeighingMS.Domain.Audit;

namespace WeighingMS.Api.IntegrationTests;

public sealed class StepUpTests(ApiFixture fixture) : IAsyncLifetime
{
    private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _api = fixture.CreateApi();

    public async ValueTask InitializeAsync() => await fixture.ResetAsync();

    public async ValueTask DisposeAsync() => await _api.DisposeAsync();

    private async Task<StationClient> SignedInAsync()
    {
        var client = new StationClient(_api);
        (await client.SignInAsync()).EnsureSuccessStatusCode();
        return client;
    }

    private static Task<HttpResponseMessage> StepUpAsync(StationClient client, string badge, string pin, string action = "override") =>
        client.SendAsync(HttpMethod.Post, "/api/session/step-up", new { badgeId = badge, pin, action });

    [Fact]
    public async Task StepUp_CorrectSupervisorPin_ReturnsSingleUseGrantStoredWithoutPin()
    {
        using var client = await SignedInAsync();

        var response = await StepUpAsync(client, "SV-2001", "2468");
        var json = await response.ReadJsonAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        json.GetProperty("supervisorName").GetString().ShouldBe("M. Rahman");
        json.GetProperty("action").GetString().ShouldBe("override");
        json.GetProperty("expiresAt").GetDateTimeOffset().ShouldBe(fixture.Clock.GetUtcNow().AddMinutes(2));
        await using var db = fixture.NewDbContext();
        var supervisor = await db.StationUsers.SingleAsync(u => u.BadgeId == "SV-2001", TestContext.Current.CancellationToken);
        supervisor.PinHash.ShouldNotBeNull();
        supervisor.PinHash.ShouldNotContain("2468");
        (await db.StepUpGrants.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task StepUp_WrongPin_Returns403AndIsAudited()
    {
        using var client = await SignedInAsync();

        var response = await StepUpAsync(client, "SV-2001", "0000");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await response.ErrorCodeAsync()).ShouldBe("Auth.StepUpFailed");
        await using var db = fixture.NewDbContext();
        (await db.AuditEntries.CountAsync(e => e.Type == AuditEventType.StepUpFailed, TestContext.Current.CancellationToken)).ShouldBe(1);
        (await db.StepUpGrants.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task StepUp_OperatorBadge_Returns403SupervisorRequired()
    {
        using var client = await SignedInAsync();

        (await (await StepUpAsync(client, "OP-1002", "1234")).ErrorCodeAsync()).ShouldBe("Auth.SupervisorRequired");
    }

    [Fact]
    public async Task StepUp_FiveWrongPins_Returns429LockedOutAndLockSurvivesRestart()
    {
        using var client = await SignedInAsync();
        for (var i = 0; i < 4; i++)
        {
            (await StepUpAsync(client, "SV-2001", "0000")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        var locked = await StepUpAsync(client, "SV-2001", "0000");
        locked.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        (await locked.ErrorCodeAsync()).ShouldBe("Auth.LockedOut");

        await using var restarted = fixture.CreateApi();
        using var other = new StationClient(restarted);
        (await other.SignInAsync()).EnsureSuccessStatusCode();
        (await (await StepUpAsync(other, "SV-2001", "2468")).ErrorCodeAsync()).ShouldBe("Auth.LockedOut");

        fixture.Clock.Advance(TimeSpan.FromMinutes(5));
        (await StepUpAsync(other, "SV-2001", "2468")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StepUp_InvalidInput_Returns422()
    {
        using var client = await SignedInAsync();

        var response = await StepUpAsync(client, "SV-2001", "12", "fly");
        var errors = (await response.ReadJsonAsync()).GetProperty("errors");

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        errors.TryGetProperty("Pin", out _).ShouldBeTrue();
        errors.TryGetProperty("Action", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task StepUp_WithoutSession_Returns401()
    {
        using var client = new StationClient(_api);
        await client.PrimeAntiforgeryAsync();

        (await StepUpAsync(client, "SV-2001", "2468")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Grant_ConsumedThroughTheService_CanBeUsedOnlyOnce()
    {
        using var client = await SignedInAsync();
        var grantId = (await (await StepUpAsync(client, "SV-2001", "2468")).ReadJsonAsync()).GetProperty("grantId").GetGuid();

        await using (var scope = _api.Services.CreateAsyncScope())
        {
            var access = scope.ServiceProvider.GetRequiredService<StationAccessService>();
            (await access.ConsumeGrantAsync(grantId, "override", TestContext.Current.CancellationToken)).IsError.ShouldBeFalse();
            await scope.ServiceProvider.GetRequiredService<WeighingMS.Application.Abstractions.IUnitOfWork>().SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var again = _api.Services.CreateAsyncScope();
        var second = await again.ServiceProvider.GetRequiredService<StationAccessService>().ConsumeGrantAsync(grantId, "override", TestContext.Current.CancellationToken);
        second.FirstError.Code.ShouldBe("Auth.GrantInvalid");
    }

    [Fact]
    public async Task RateLimiter_TooManyAttemptsPerMinute_Returns429TooManyAttempts()
    {
        await using var strict = fixture.CreateApi(new Dictionary<string, string?> { ["RateLimits:AuthPermitLimitPerMinute"] = "2" });
        using var client = new StationClient(strict);
        await client.PrimeAntiforgeryAsync();

        await client.SendAsync(HttpMethod.Post, "/api/session", new { badgeId = "OP-9999" });
        await client.SendAsync(HttpMethod.Post, "/api/session", new { badgeId = "OP-9999" });
        var third = await client.SendAsync(HttpMethod.Post, "/api/session", new { badgeId = "OP-1001" });

        third.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        (await third.ErrorCodeAsync()).ShouldBe("Auth.TooManyAttempts");
    }
}
