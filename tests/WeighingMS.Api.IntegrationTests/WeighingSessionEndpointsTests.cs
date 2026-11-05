using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using WeighingMS.Api.Common;
using WeighingMS.Domain.Audit;
using WeighingMS.Domain.Weighing;
using WeighingMS.Infrastructure.Weighing;

namespace WeighingMS.Api.IntegrationTests;

/// <summary>Bolt 007 through the Api: PostgreSQL 17 (Testcontainers) and the real simulator in-process.</summary>
public sealed class WeighingSessionEndpointsTests(ApiFixture fixture) : IAsyncLifetime
{
    private static readonly TimeSpan _pour = TimeSpan.FromSeconds(8);
    private readonly SimulatorHost _simulator = new();
    private WebApplicationFactory<Program> _api = null!;
    private StationClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        _api = fixture.CreateApi(simulator: _simulator);
        _client = new StationClient(_api);
        (await _client.SignInAsync()).EnsureSuccessStatusCode();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _api.DisposeAsync();
        await _simulator.DisposeAsync();
    }

    private Task<HttpResponseMessage> StartAsync(string batchId = "B-24187") => _client.SendAsync(HttpMethod.Post, $"/api/batches/{batchId}/session");

    private Task<HttpResponseMessage> SelectAsync(int sequence, string batchId = "B-24187") =>
        _client.SendAsync(HttpMethod.Post, $"/api/batches/{batchId}/session/selection", new { sequence });

    private static async Task<JsonElement> OkJsonAsync(Task<HttpResponseMessage> call, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var response = await call;
        response.StatusCode.ShouldBe(expected, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return await response.ReadJsonAsync();
    }

    private Task<JsonElement> ReadingAsync() => OkJsonAsync(_client.GetAsync("/api/batches/B-24187/session/reading"));

    private async Task PourAsync(string scaleId, string scenario, decimal target, string unit, decimal? tolerance = null)
    {
        (await _client.SendAsync(HttpMethod.Post, $"/api/scales/{scaleId}/tare")).EnsureSuccessStatusCode();
        await _simulator.PostAsync($"/_sim/scales/{scaleId}/scenario", new { scenario, target = new { value = target, unit }, tolerance });
        _simulator.Clock.Advance(_pour);
    }

    private static decimal Value(JsonElement quantity) => quantity.GetProperty("value").GetDecimal();

    [Fact]
    public async Task Start_FirstOpen_Is201WithEveryComponentPendingAndItsScaleAndBand()
    {
        var response = await StartAsync();
        var session = await OkJsonAsync(Task.FromResult(response), HttpStatusCode.Created);

        response.Headers.Location!.ToString().ShouldEndWith("/api/batches/B-24187/session");
        session.GetProperty("batchId").GetString().ShouldBe("B-24187");
        session.GetProperty("status").GetString().ShouldBe("inProgress");
        session.GetProperty("startedBy").GetString().ShouldBe("OP-1001");
        session.GetProperty("progress").GetProperty("done").GetInt32().ShouldBe(0);
        session.GetProperty("progress").GetProperty("total").GetInt32().ShouldBe(5);
        session.GetProperty("suggestedSequence").GetInt32().ShouldBe(1);
        session.GetProperty("activeSequence").ValueKind.ShouldBe(JsonValueKind.Null);

        var components = session.GetProperty("components").EnumerateArray().ToList();
        components.Select(c => c.GetProperty("state").GetString()).ShouldAllBe(s => s == "pending");
        components.Select(c => c.GetProperty("scale").GetProperty("scaleId").GetString())
            .ShouldBe(["scale-3kg", "scale-3kg", "scale-3kg", "scale-32kg", "scale-32kg"]);

        var yellow = components[2];
        yellow.GetProperty("name").GetString().ShouldBe("Remazol Yellow RGB");
        Value(yellow.GetProperty("target")).ShouldBe(94.0m);
        yellow.GetProperty("scale").GetProperty("scaleClass").GetString().ShouldBe("precision");
        yellow.GetProperty("scale").GetProperty("unit").GetString().ShouldBe("g");
        Value(yellow.GetProperty("scale").GetProperty("resolution")).ShouldBe(0.1m);
        yellow.GetProperty("scale").GetProperty("automatic").GetBoolean().ShouldBeTrue();
        var tolerance = yellow.GetProperty("tolerance");
        tolerance.GetProperty("status").GetString().ShouldBe("resolved");
        tolerance.GetProperty("source").GetString().ShouldBe("recipe");
        Value(tolerance.GetProperty("plusMinus")).ShouldBe(0.5m);
        Value(tolerance.GetProperty("lower")).ShouldBe(93.5m);
        Value(tolerance.GetProperty("upper")).ShouldBe(94.5m);
        yellow.GetProperty("suggestedContainer").GetString().ShouldBe("C-12");

        var sulphate = components[3];
        sulphate.GetProperty("scale").GetProperty("scaleClass").GetString().ShouldBe("heavyDuty");
        Value(sulphate.GetProperty("tolerance").GetProperty("lower")).ShouldBe(23.880m);
    }

    [Fact]
    public async Task Start_Again_Is200ResumeAndChangesNothing()
    {
        await OkJsonAsync(StartAsync(), HttpStatusCode.Created);
        await OkJsonAsync(SelectAsync(3));

        var resumed = await OkJsonAsync(StartAsync());

        resumed.GetProperty("activeSequence").GetInt32().ShouldBe(3);
        await using var db = fixture.NewDbContext();
        (await db.WeighingSessions.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task Session_SurvivesAnApiRestartAndAnotherOperatorResumesIt()
    {
        await OkJsonAsync(StartAsync(), HttpStatusCode.Created);
        await OkJsonAsync(SelectAsync(4));
        _client.Dispose();
        await _api.DisposeAsync();

        _api = fixture.CreateApi(simulator: _simulator);
        _client = new StationClient(_api);
        (await _client.SignInAsync("OP-1002")).EnsureSuccessStatusCode();
        var resumed = await OkJsonAsync(StartAsync());

        resumed.GetProperty("startedBy").GetString().ShouldBe("OP-1001");
        resumed.GetProperty("activeSequence").GetInt32().ShouldBe(4);
        resumed.GetProperty("components").EnumerateArray().Select(c => c.GetProperty("state").GetString())
            .ShouldBe(["pending", "pending", "pending", "active", "pending"]);
    }

    [Fact]
    public async Task Start_SixConcurrentFirstOpens_StoreOneSession()
    {
        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => StartAsync()));

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(5);
        await using var db = fixture.NewDbContext();
        (await db.WeighingSessions.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
        (await db.AuditEntries.CountAsync(a => a.Type == AuditEventType.WeighingSessionStarted, TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task Start_UnknownBatch_Is404BatchNotFound()
    {
        var response = await StartAsync("B-00000");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.ErrorCodeAsync()).ShouldBe("Batch.NotFound");
    }

    [Fact]
    public async Task Start_ScaleListDown_Is503AndNothingIsStored()
    {
        await _simulator.FaultAsync("scale:scale-3kg", "unavailable");
        await _simulator.FaultAsync("scale:scale-32kg", "unavailable");
        await _simulator.FaultAsync("orgatex", "unavailable");

        var response = await StartAsync("B-24188");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        await using var db = fixture.NewDbContext();
        (await db.WeighingSessions.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task Start_ConfiguredThreshold_DecidesTheScaleWithoutACodeChange()
    {
        _client.Dispose();
        await _api.DisposeAsync();
        _api = fixture.CreateApi(new Dictionary<string, string?> { ["Weighing:ScaleSelection:ThresholdKg"] = "0.05" }, _simulator);
        _client = new StationClient(_api);
        (await _client.SignInAsync()).EnsureSuccessStatusCode();

        var session = await OkJsonAsync(StartAsync(), HttpStatusCode.Created);

        // 94.0 g = 0.094 kg > 0.05 kg → the floor scale now
        session.GetProperty("components")[2].GetProperty("scale").GetProperty("scaleId").GetString().ShouldBe("scale-32kg");
    }

    [Fact]
    public async Task Get_NoSession_Is404()
    {
        var response = await _client.GetAsync("/api/batches/B-24187/session");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.ErrorCodeAsync()).ShouldBe("Weighing.SessionNotFound");
    }

    [Fact]
    public async Task Select_SwitchingComponent_AbandonsTheFirstAndAuditsBoth()
    {
        await OkJsonAsync(StartAsync(), HttpStatusCode.Created);
        await OkJsonAsync(SelectAsync(3));

        var session = await OkJsonAsync(SelectAsync(5));

        session.GetProperty("activeSequence").GetInt32().ShouldBe(5);
        session.GetProperty("components")[2].GetProperty("state").GetString().ShouldBe("pending");
        session.GetProperty("components")[4].GetProperty("state").GetString().ShouldBe("active");
        await using var db = fixture.NewDbContext();
        var audit = await db.AuditEntries.Where(a => a.Subject != null && a.Subject.StartsWith("B-24187#"))
            .Select(a => new { a.Type, a.Subject }).ToListAsync(TestContext.Current.CancellationToken);

        // Entries of one request share the same instant, and UUIDv7s made in the same millisecond are not ordered,
        // so the switch is checked as a set: one abandon of 3 and one selection each of 3 and 5.
        audit.Select(a => (a.Type, a.Subject)).ShouldBe(
            [
                (AuditEventType.ComponentSelected, "B-24187#3"),
                (AuditEventType.AttemptAbandoned, "B-24187#3"),
                (AuditEventType.ComponentSelected, "B-24187#5"),
            ],
            ignoreOrder: true);
    }

    [Fact]
    public async Task Select_UnknownComponent_Is404AndMissingSequenceIs422()
    {
        await OkJsonAsync(StartAsync(), HttpStatusCode.Created);

        var unknown = await SelectAsync(9);
        var missing = await _client.SendAsync(HttpMethod.Post, "/api/batches/B-24187/session/selection", new { });

        unknown.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await unknown.ErrorCodeAsync()).ShouldBe("Weighing.ComponentNotFound");
        missing.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await missing.ErrorCodeAsync()).ShouldBe("Request.Invalid");
    }

    [Fact]
    public async Task Reading_NoActiveComponent_Is409()
    {
        await OkJsonAsync(StartAsync(), HttpStatusCode.Created);

        var response = await _client.GetAsync("/api/batches/B-24187/session/reading");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.ErrorCodeAsync()).ShouldBe("Weighing.NoActiveComponent");
    }

    [Theory]
    [InlineData("belowRange", 93.0, "underweight", 1.0)]
    [InlineData("enterRange", 94.2, "withinRange", 0.0)]
    [InlineData("overshoot", 96.4, "overweight", 0.0)]
    [InlineData("rising", 94.0, "targetAchieved", 0.0)]
    public async Task Reading_YellowRgb_IsClassifiedOnTheServer(string scenario, decimal net, string expectedClass, decimal remaining)
    {
        await OkJsonAsync(StartAsync(), HttpStatusCode.Created);
        await OkJsonAsync(SelectAsync(3));
        await PourAsync("scale-3kg", scenario, 94.0m, "g", scenario == "rising" ? null : 0.5m);

        var reading = await ReadingAsync();

        reading.GetProperty("sequence").GetInt32().ShouldBe(3);
        reading.GetProperty("scaleId").GetString().ShouldBe("scale-3kg");
        Value(reading.GetProperty("net")).ShouldBe(net);
        reading.GetProperty("net").GetProperty("unit").GetString().ShouldBe("g");
        reading.GetProperty("stable").GetBoolean().ShouldBeTrue();
        reading.GetProperty("validity").GetString().ShouldBe("valid");
        reading.GetProperty("class").GetString().ShouldBe(expectedClass);
        Value(reading.GetProperty("remaining")).ShouldBe(remaining);
        reading.GetProperty("toleranceStatus").GetString().ShouldBe("resolved");
        var band = reading.GetProperty("band");
        Value(band.GetProperty("target")).ShouldBe(94.0m);
        Value(band.GetProperty("plusMinus")).ShouldBe(0.5m);
        Value(band.GetProperty("lower")).ShouldBe(93.5m);
        Value(band.GetProperty("upper")).ShouldBe(94.5m);
    }

    [Fact]
    public async Task Reading_IsTakenFromTheActiveComponentsOwnScale()
    {
        await OkJsonAsync(StartAsync(), HttpStatusCode.Created);
        await OkJsonAsync(SelectAsync(4));
        await PourAsync("scale-32kg", "enterRange", 24.000m, "kg", 0.120m);
        await PourAsync("scale-3kg", "overshoot", 94.0m, "g", 0.5m); // another scale's weight must not matter

        var reading = await ReadingAsync();

        reading.GetProperty("scaleId").GetString().ShouldBe("scale-32kg");
        Value(reading.GetProperty("net")).ShouldBe(24.048m);
        reading.GetProperty("net").GetProperty("unit").GetString().ShouldBe("kg");
        reading.GetProperty("class").GetString().ShouldBe("withinRange");
    }

    [Fact]
    public async Task Reading_WhileStillPouring_IsUnstableAndUnderweightWithRemaining()
    {
        await OkJsonAsync(StartAsync(), HttpStatusCode.Created);
        await OkJsonAsync(SelectAsync(3));
        (await _client.SendAsync(HttpMethod.Post, "/api/scales/scale-3kg/tare")).EnsureSuccessStatusCode();
        await _simulator.PostAsync("/_sim/scales/scale-3kg/scenario", new { scenario = "enterRange", target = new { value = 94.0m, unit = "g" }, tolerance = 0.5m });
        _simulator.Clock.Advance(TimeSpan.FromSeconds(2));

        var reading = await ReadingAsync();

        reading.GetProperty("stable").GetBoolean().ShouldBeFalse();
        reading.GetProperty("class").GetString().ShouldBe("underweight");
        (Value(reading.GetProperty("net")) + Value(reading.GetProperty("remaining"))).ShouldBe(94.0m);
    }

    [Fact]
    public async Task Reading_ScaleFaulted_Is503()
    {
        await OkJsonAsync(StartAsync(), HttpStatusCode.Created);
        await OkJsonAsync(SelectAsync(3));
        await _simulator.FaultAsync("scale:scale-3kg", "unavailable");

        var response = await _client.GetAsync("/api/batches/B-24187/session/reading");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await response.ErrorCodeAsync()).ShouldBe("Scale.Unavailable");
    }

    [Fact]
    public async Task TwoConcurrentChangesToOneSession_TheSecondIsAConflict()
    {
        await OkJsonAsync(StartAsync(), HttpStatusCode.Created);
        var ct = TestContext.Current.CancellationToken;
        await using var dbA = fixture.NewDbContext();
        await using var dbB = fixture.NewDbContext();
        var stationA = new WeighingSessionRepository(dbA);
        var stationB = new WeighingSessionRepository(dbB);
        var a = (await stationA.FindAsync("B-24187", ct))!;
        var b = (await stationB.FindAsync("B-24187", ct))!;

        a.Select(2, "OP-1001", DateTimeOffset.UtcNow);
        b.Select(3, "OP-1002", DateTimeOffset.UtcNow);
        var first = await stationA.SaveAsync(ct);
        var second = await stationB.SaveAsync(ct);

        first.IsError.ShouldBeFalse();
        second.FirstError.Code.ShouldBe("Concurrency.Conflict");
        await using var check = fixture.NewDbContext();
        (await check.WeighingSessions.SingleAsync(ct)).ActiveSequence.ShouldBe(2);
    }

    [Fact]
    public async Task Endpoints_WithoutSession_Are401()
    {
        using var anonymous = _api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var ct = TestContext.Current.CancellationToken;

        (await anonymous.GetAsync(new Uri("/api/batches/B-24187/session", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync(new Uri("/api/batches/B-24187/session/reading", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Commands_WithoutXsrf_Are400()
    {
        var ct = TestContext.Current.CancellationToken;

        var start = await _client.Http.PostAsync(new Uri("/api/batches/B-24187/session", UriKind.Relative), null, ct);
        var select = await _client.Http.PostAsync(new Uri("/api/batches/B-24187/session/selection", UriKind.Relative), System.Net.Http.Json.JsonContent.Create(new { sequence = 1 }), ct);

        start.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        select.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("GET", "/api/batches/B-24187/session/reading", true)]
    [InlineData("GET", "/api/scales/scale-3kg/weight", true)]
    [InlineData("GET", "/api/batches/B-24187/session", false)]
    [InlineData("POST", "/api/batches/B-24187/session/reading", false)]
    public void ReadingPolls_AreTreatedAsHealthyWeightPolls(string method, string path, bool poll) =>
        ObservabilityExtensions.IsWeightPoll(method, path).ShouldBe(poll);

    [Fact]
    public async Task Migration_CreatesTheSessionTables()
    {
        await using var db = fixture.NewDbContext();
        (await db.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).ShouldContain(m => m.EndsWith("_AddWeighingSessions", StringComparison.Ordinal));
        (await db.WeighingSessions.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
        typeof(WeighingSession).ShouldNotBeNull();
    }
}
