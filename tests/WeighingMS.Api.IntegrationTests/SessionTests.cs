using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using WeighingMS.Domain.Audit;

namespace WeighingMS.Api.IntegrationTests;

public sealed class SessionTests(ApiFixture fixture) : IAsyncLifetime
{
    private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _api = fixture.CreateApi();

    public async ValueTask InitializeAsync() => await fixture.ResetAsync();

    public async ValueTask DisposeAsync() => await _api.DisposeAsync();

    [Fact]
    public async Task SignIn_KnownBadge_Returns200AndASecureHttpOnlySessionCookie()
    {
        using var client = new StationClient(_api);

        var response = await client.SignInAsync(" op-1001 ");
        var json = await response.ReadJsonAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        json.GetProperty("badgeId").GetString().ShouldBe("OP-1001");
        json.GetProperty("displayName").GetString().ShouldBe("R. Karim");
        json.GetProperty("role").GetString().ShouldBe("operator");
        json.GetProperty("shift").GetString().ShouldBe("A");
        json.GetProperty("stationId").GetString().ShouldBe("STATION 01");
        json.GetProperty("timeZone").GetString().ShouldBe("Asia/Dhaka");
        var session = client.Cookies["__Host-wms.session"];
        session.ShouldNotBeNull();
        session.HttpOnly.ShouldBeTrue();
        session.Secure.ShouldBeTrue();
        response.Headers.CacheControl!.NoStore.ShouldBeTrue();
    }

    [Fact]
    public async Task SignIn_UnknownBadge_Returns401ProblemAndNoSession()
    {
        using var client = new StationClient(_api);

        var response = await client.SignInAsync("OP-9999");
        var problem = await response.ReadJsonAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        problem.GetProperty("type").GetString().ShouldBe("Auth.BadgeNotRecognised");
        problem.GetProperty("errorCode").GetString().ShouldBe("Auth.BadgeNotRecognised");
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        client.Cookies["__Host-wms.session"].ShouldBeNull();
        (await client.GetAsync("/api/session")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SignIn_InvalidBadge_Returns422WithFieldErrors()
    {
        using var client = new StationClient(_api);

        var response = await client.SignInAsync("OP 1001!");
        var problem = await response.ReadJsonAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        problem.GetProperty("errorCode").GetString().ShouldBe("Request.Invalid");
        problem.GetProperty("errors").TryGetProperty("BadgeId", out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData("/api/session")]
    [InlineData("/api/station")]
    public async Task ProtectedEndpoints_WithoutSession_Return401SignInRequired(string url)
    {
        using var client = new StationClient(_api);

        var response = await client.GetAsync(url);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.ErrorCodeAsync()).ShouldBe("Auth.SignInRequired");
    }

    [Fact]
    public async Task Session_AfterSignIn_IsReturnedUntilSignOut()
    {
        using var client = new StationClient(_api);
        await client.SignInAsync();

        (await (await client.GetAsync("/api/session")).ReadJsonAsync()).GetProperty("displayName").GetString().ShouldBe("R. Karim");
        (await client.SendAsync(HttpMethod.Delete, "/api/session")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.GetAsync("/api/session")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SignIn_WhileSignedIn_ReplacesTheSessionAndAuditsIt()
    {
        using var client = new StationClient(_api);
        await client.SignInAsync("OP-1001");

        var response = await client.SendAsync(HttpMethod.Post, "/api/session", new { badgeId = "OP-1002" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await (await client.GetAsync("/api/session")).ReadJsonAsync()).GetProperty("badgeId").GetString().ShouldBe("OP-1002");
        await using var db = fixture.NewDbContext();
        var replaced = await db.AuditEntries.SingleAsync(e => e.Type == AuditEventType.SessionReplaced, TestContext.Current.CancellationToken);
        replaced.ActorBadgeId.ShouldBe("OP-1002");
        replaced.Subject.ShouldBe("OP-1001");
    }

    [Fact]
    public async Task StateChangingRequest_WithoutXsrfToken_Returns400AntiforgeryInvalid()
    {
        using var client = new StationClient(_api);

        var response = await client.Http.PostAsJsonAsync("/api/session", new { badgeId = "OP-1001" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ErrorCodeAsync()).ShouldBe("Request.AntiforgeryInvalid");
    }

    [Fact]
    public async Task XsrfToken_FromBeforeSignIn_IsReplacedOnSignIn()
    {
        using var client = new StationClient(_api);
        await client.PrimeAntiforgeryAsync();
        var before = client.XsrfToken;

        await client.SendAsync(HttpMethod.Post, "/api/session", new { badgeId = "OP-1001" });

        client.XsrfToken.ShouldNotBe(before);
        (await client.SendAsync(HttpMethod.Delete, "/api/session")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SignInAndOut_AreAuditedInUtc()
    {
        using var client = new StationClient(_api);
        await client.SignInAsync();
        await client.SendAsync(HttpMethod.Delete, "/api/session");

        await using var db = fixture.NewDbContext();
        var entries = await db.AuditEntries.OrderBy(e => e.OccurredAt).ThenBy(e => e.Id).ToListAsync(TestContext.Current.CancellationToken);

        entries.Select(e => e.Type).ShouldBe([AuditEventType.SignIn, AuditEventType.SignOut]);
        entries.ShouldAllBe(e => e.OccurredAt == fixture.Clock.GetUtcNow() && e.ActorBadgeId == "OP-1001");
    }
}
