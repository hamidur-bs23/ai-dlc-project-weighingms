using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using WeighingMS.Domain.Audit;
using WeighingMS.Domain.Weighing;

namespace WeighingMS.Api.IntegrationTests;

/// <summary>Bolt 008 through the Api: PostgreSQL 17 (Testcontainers) and the real simulator in-process.</summary>
public sealed class WeighingConfirmationEndpointsTests(ApiFixture fixture) : IAsyncLifetime
{
    private static readonly TimeSpan _pour = TimeSpan.FromSeconds(8);
    private readonly SimulatorHost _simulator = new();
    private WebApplicationFactory<Program> _api = null!;
    private StationClient _client = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        _api = fixture.CreateApi(simulator: _simulator);
        _client = new StationClient(_api);
        (await _client.SignInAsync()).EnsureSuccessStatusCode();
        (await Post("")).StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _api.DisposeAsync();
        await _simulator.DisposeAsync();
    }

    private Task<HttpResponseMessage> Post(string path, object? body = null) =>
        _client.SendAsync(HttpMethod.Post, $"/api/batches/B-24187/session{path}", body);

    private static async Task<JsonElement> Expect(Task<HttpResponseMessage> call, HttpStatusCode status)
    {
        var response = await call;
        response.StatusCode.ShouldBe(status, await response.Content.ReadAsStringAsync(Ct));
        return await response.ReadJsonAsync();
    }

    private Task<JsonElement> ReadingAsync() => Expect(_client.GetAsync("/api/batches/B-24187/session/reading"), HttpStatusCode.OK);

    private static decimal Value(JsonElement q) => q.GetProperty("value").GetDecimal();

    private static List<string> Missing(JsonElement problem) => [.. problem.GetProperty("errors").EnumerateObject().Select(p => p.Name)];

    /// <summary>Selects, captures lot and container, tares through the session, then pours to a scenario.</summary>
    private async Task PrepareAsync(int sequence, string scaleId, string scenario, decimal target, string unit, decimal? tolerance = null, string lot = "2026-0387", string container = "C-12")
    {
        await _simulator.PostAsync("/_sim/reset", new { });
        await Expect(Post("/selection", new { sequence }), HttpStatusCode.OK);
        await Expect(Post("/attempt/lot", new { sequence, lot }), HttpStatusCode.OK);
        await Expect(Post("/attempt/container", new { sequence, container }), HttpStatusCode.OK);
        await Expect(Post("/tare", new { sequence }), HttpStatusCode.OK);
        await _simulator.PostAsync($"/_sim/scales/{scaleId}/scenario", new { scenario, target = new { value = target, unit }, tolerance });
        _simulator.Clock.Advance(_pour);
    }

    private Task PrepareYellowAsync(string scenario = "enterRange") => PrepareAsync(3, "scale-3kg", scenario, 94.0m, "g", scenario == "rising" ? null : 0.5m);

    [Fact]
    public async Task Step25_Confirm_RecordsEveryFieldOnceAndActivatesTheNextComponent()
    {
        await PrepareYellowAsync();
        var reading = await ReadingAsync();
        reading.GetProperty("confirmation").GetProperty("canConfirm").GetBoolean().ShouldBeTrue();

        var response = await Post("/confirmation", new { sequence = 3 });
        var body = await Expect(Task.FromResult(response), HttpStatusCode.Created);

        var record = body.GetProperty("record");
        record.GetProperty("batchId").GetString().ShouldBe("B-24187");
        record.GetProperty("sequence").GetInt32().ShouldBe(3);
        record.GetProperty("chemicalId").GetString().ShouldBe("DYE-4471");
        record.GetProperty("chemicalName").GetString().ShouldBe("Remazol Yellow RGB");
        Value(record.GetProperty("required")).ShouldBe(94.0m);
        Value(record.GetProperty("actual")).ShouldBe(94.2m);
        record.GetProperty("actual").GetProperty("unit").GetString().ShouldBe("g");
        record.GetProperty("class").GetString().ShouldBe("withinRange");
        Value(record.GetProperty("tolerance").GetProperty("plusMinus")).ShouldBe(0.5m);
        record.GetProperty("tolerance").GetProperty("source").GetString().ShouldBe("recipe");
        record.GetProperty("scaleId").GetString().ShouldBe("scale-3kg");
        record.GetProperty("scaleClass").GetString().ShouldBe("precision");
        record.GetProperty("lot").GetString().ShouldBe("2026-0387");
        record.GetProperty("container").GetString().ShouldBe("C-12");
        Value(record.GetProperty("tare")).ShouldBe(214.0m);
        record.GetProperty("operator").GetString().ShouldBe("OP-1001");
        record.GetProperty("confirmedAt").GetDateTimeOffset().ShouldBe(fixture.Clock.GetUtcNow());

        var session = body.GetProperty("session");
        session.GetProperty("activeSequence").GetInt32().ShouldBe(4);
        session.GetProperty("progress").GetProperty("done").GetInt32().ShouldBe(1);
        var yellow = session.GetProperty("components")[2];
        yellow.GetProperty("state").GetString().ShouldBe("done");
        Value(yellow.GetProperty("record").GetProperty("actual")).ShouldBe(94.2m);
        yellow.GetProperty("attempt").ValueKind.ShouldBe(JsonValueKind.Null);
        session.GetProperty("components")[3].GetProperty("attempt").GetProperty("lot").ValueKind.ShouldBe(JsonValueKind.Null);

        await using var db = fixture.NewDbContext();
        var stored = await db.WeighingRecords.SingleAsync(Ct);
        stored.Actual.Value.ShouldBe(94.2m);
        stored.Tare.Value.ShouldBe(214.0m);
        stored.OperatorBadgeId.ShouldBe("OP-1001");
        (await db.AuditEntries.CountAsync(a => a.Type == AuditEventType.WeighingConfirmed && a.Subject == "B-24187#3", Ct)).ShouldBe(1);
    }

    [Fact]
    public async Task WholeBatch_FiveConfirms_FollowTheRecipeAndEndReadyToComplete()
    {
        var plan = new (int Seq, string Scale, decimal Target, string Unit)[]
        {
            (1, "scale-3kg", 1240.0m, "g"), (2, "scale-3kg", 386.0m, "g"), (3, "scale-3kg", 94.0m, "g"),
            (4, "scale-32kg", 24.000m, "kg"), (5, "scale-32kg", 9.600m, "kg"),
        };

        JsonElement session = default;
        foreach (var (seq, scale, target, unit) in plan)
        {
            await PrepareAsync(seq, scale, "rising", target, unit);
            var body = await Expect(Post("/confirmation", new { sequence = seq }), HttpStatusCode.Created);
            body.GetProperty("record").GetProperty("class").GetString().ShouldBe("targetAchieved");
            session = body.GetProperty("session");
        }

        session.GetProperty("readyToComplete").GetBoolean().ShouldBeTrue();
        session.GetProperty("activeSequence").ValueKind.ShouldBe(JsonValueKind.Null);
        session.GetProperty("progress").GetProperty("done").GetInt32().ShouldBe(5);
        await using var db = fixture.NewDbContext();
        (await db.WeighingRecords.CountAsync(Ct)).ShouldBe(5);
        (await db.WeighingRecords.Select(r => r.ScaleId).ToListAsync(Ct)).Count(s => s == "scale-32kg").ShouldBe(2);
    }

    [Fact]
    public async Task Confirm_SixConcurrentRequests_WriteOneRecord()
    {
        await PrepareYellowAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Post("/confirmation", new { sequence = 3 })));

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(5);
        await using var db = fixture.NewDbContext();
        (await db.WeighingRecords.CountAsync(Ct)).ShouldBe(1);
        (await db.AuditEntries.CountAsync(a => a.Type == AuditEventType.WeighingConfirmed, Ct)).ShouldBe(1);
    }

    [Fact]
    public async Task Confirm_DoubleTap_SecondIs409AlreadyConfirmed()
    {
        await PrepareYellowAsync();
        await Expect(Post("/confirmation", new { sequence = 3 }), HttpStatusCode.Created);

        var second = await Post("/confirmation", new { sequence = 3 });

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await second.ErrorCodeAsync()).ShouldBe("Weighing.AlreadyConfirmed");
    }

    [Fact]
    public async Task Confirm_NothingCaptured_Is422ListingWhatIsMissing()
    {
        await Expect(Post("/selection", new { sequence = 3 }), HttpStatusCode.OK);

        var problem = await Expect(Post("/confirmation", new { sequence = 3 }), HttpStatusCode.UnprocessableEntity);

        problem.GetProperty("errorCode").GetString().ShouldBe("Weighing.NotConfirmable");
        Missing(problem).ShouldBe(["lot", "container", "tare", "inTolerance"], ignoreOrder: true);
    }

    [Theory]
    [InlineData("overshoot", "Weighing.OutOfTolerance")]
    [InlineData("belowRange", "Weighing.OutOfTolerance")]
    public async Task Confirm_OutOfTolerance_Is422AndNoRecord(string scenario, string code)
    {
        await PrepareYellowAsync(scenario);

        var problem = await Expect(Post("/confirmation", new { sequence = 3 }), HttpStatusCode.UnprocessableEntity);

        problem.GetProperty("errorCode").GetString().ShouldBe(code);
        Missing(problem).ShouldBe(["inTolerance"]);
        await using var db = fixture.NewDbContext();
        (await db.WeighingRecords.CountAsync(Ct)).ShouldBe(0);
    }

    [Fact]
    public async Task Confirm_StillPouring_Is422Stable()
    {
        await _simulator.PostAsync("/_sim/reset", new { });
        await Expect(Post("/selection", new { sequence = 3 }), HttpStatusCode.OK);
        await Expect(Post("/attempt/lot", new { sequence = 3, lot = "L1" }), HttpStatusCode.OK);
        await Expect(Post("/attempt/container", new { sequence = 3, container = "C-12" }), HttpStatusCode.OK);
        await Expect(Post("/tare", new { sequence = 3 }), HttpStatusCode.OK);
        await _simulator.PostAsync("/_sim/scales/scale-3kg/scenario", new { scenario = "enterRange", target = new { value = 94.0m, unit = "g" }, tolerance = 0.5m });
        _simulator.Clock.Advance(TimeSpan.FromSeconds(2));

        var reading = await ReadingAsync();
        var problem = await Expect(Post("/confirmation", new { sequence = 3 }), HttpStatusCode.UnprocessableEntity);

        reading.GetProperty("confirmation").GetProperty("missing").EnumerateArray().Select(m => m.GetString()).ShouldContain("stable");
        Missing(problem).ShouldContain("stable");
    }

    [Fact]
    public async Task Tare_ThroughTheDeviceEndpoint_DoesNotCountForTheAttempt() // SESSION-TARE
    {
        await _simulator.PostAsync("/_sim/reset", new { });
        await Expect(Post("/selection", new { sequence = 3 }), HttpStatusCode.OK);
        await Expect(Post("/attempt/lot", new { sequence = 3, lot = "L1" }), HttpStatusCode.OK);
        await Expect(Post("/attempt/container", new { sequence = 3, container = "C-12" }), HttpStatusCode.OK);
        (await _client.SendAsync(HttpMethod.Post, "/api/scales/scale-3kg/tare")).EnsureSuccessStatusCode();
        await _simulator.PostAsync("/_sim/scales/scale-3kg/scenario", new { scenario = "enterRange", target = new { value = 94.0m, unit = "g" }, tolerance = 0.5m });
        _simulator.Clock.Advance(_pour);

        var problem = await Expect(Post("/confirmation", new { sequence = 3 }), HttpStatusCode.UnprocessableEntity);

        Missing(problem).ShouldBe(["tare"]);
    }

    [Fact]
    public async Task Tare_Again_ReplacesTheTareAndIsAuditedAsReTare()
    {
        await PrepareYellowAsync();

        var session = await Expect(Post("/tare", new { sequence = 3 }), HttpStatusCode.OK);

        // After the pour the scale's gross includes the poured amount, so the re-tare stores that value.
        Value(session.GetProperty("components")[2].GetProperty("attempt").GetProperty("tare")).ShouldBe(308.2m);
        await using var db = fixture.NewDbContext();
        (await db.AuditEntries.CountAsync(a => a.Type == AuditEventType.ReTare && a.Subject == "B-24187#3", Ct)).ShouldBe(1);
        (await db.WeighingRecords.CountAsync(Ct)).ShouldBe(0);
    }

    [Fact]
    public async Task Confirm_ScaleWentOfflineAfterTheTare_Is422TareStale()
    {
        await PrepareYellowAsync();
        await _simulator.FaultAsync("scale:scale-3kg", "unavailable");
        (await _client.GetAsync("/api/batches/B-24187/session/reading")).StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        await _simulator.ClearFaultsAsync();
        fixture.Clock.Advance(TimeSpan.FromSeconds(5));

        var problem = await Expect(Post("/confirmation", new { sequence = 3 }), HttpStatusCode.UnprocessableEntity);

        Missing(problem).ShouldBe(["tareStale"]);
    }

    [Fact]
    public async Task Confirm_ScaleFaulted_Is503AndNoRecord()
    {
        await PrepareYellowAsync();
        await _simulator.FaultAsync("scale:scale-3kg", "unavailable");

        var response = await Post("/confirmation", new { sequence = 3 });

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        await using var db = fixture.NewDbContext();
        (await db.WeighingRecords.CountAsync(Ct)).ShouldBe(0);
    }

    [Fact]
    public async Task Commands_OnAPendingComponent_Are409AndInvalidInputIs422()
    {
        await Expect(Post("/selection", new { sequence = 3 }), HttpStatusCode.OK);

        var notActive = await Post("/attempt/lot", new { sequence = 4, lot = "L" });
        var tooLong = await Post("/attempt/lot", new { sequence = 3, lot = new string('x', 33) });
        var noSequence = await Post("/tare", new { });

        notActive.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await notActive.ErrorCodeAsync()).ShouldBe("Weighing.ComponentNotActive");
        tooLong.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        Missing(await tooLong.ReadJsonAsync()).ShouldBe(["lot"]);
        noSequence.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Quit_DiscardsTheAttemptWithNoRecordAndIsAudited()
    {
        await PrepareYellowAsync();

        var session = await Expect(Post("/quit", new { sequence = 3 }), HttpStatusCode.OK);

        session.GetProperty("activeSequence").ValueKind.ShouldBe(JsonValueKind.Null);
        session.GetProperty("components")[2].GetProperty("state").GetString().ShouldBe("pending");
        await using var db = fixture.NewDbContext();
        (await db.WeighingRecords.CountAsync(Ct)).ShouldBe(0);
        (await db.AuditEntries.CountAsync(a => a.Type == AuditEventType.Quit && a.Subject == "B-24187#3", Ct)).ShouldBe(1);
    }

    [Fact]
    public async Task Leave_ThenResumeAfterAnApiRestart_KeepsTheRecordAndDropsTheAttempt()
    {
        await PrepareYellowAsync();
        await Expect(Post("/confirmation", new { sequence = 3 }), HttpStatusCode.Created);
        await Expect(Post("/attempt/lot", new { sequence = 4, lot = "NS-118" }), HttpStatusCode.OK);

        var left = await Expect(Post("/leave"), HttpStatusCode.OK);
        left.GetProperty("activeSequence").ValueKind.ShouldBe(JsonValueKind.Null);

        _client.Dispose();
        await _api.DisposeAsync();
        _api = fixture.CreateApi(simulator: _simulator);
        _client = new StationClient(_api);
        (await _client.SignInAsync("OP-1002")).EnsureSuccessStatusCode();
        var resumed = await Expect(Post(""), HttpStatusCode.OK);

        resumed.GetProperty("progress").GetProperty("done").GetInt32().ShouldBe(1);
        var yellow = resumed.GetProperty("components")[2];
        Value(yellow.GetProperty("record").GetProperty("actual")).ShouldBe(94.2m);
        yellow.GetProperty("record").GetProperty("operator").GetString().ShouldBe("OP-1001");
        resumed.GetProperty("components")[3].GetProperty("state").GetString().ShouldBe("pending");
        await using var db = fixture.NewDbContext();
        (await db.AuditEntries.CountAsync(a => a.Type == AuditEventType.BatchLeft, Ct)).ShouldBe(1);
    }

    [Fact]
    public async Task Records_CannotBeChangedOrDeleted() // A9
    {
        await PrepareYellowAsync();
        await Expect(Post("/confirmation", new { sequence = 3 }), HttpStatusCode.Created);

        await using (var db = fixture.NewDbContext())
        {
            var record = await db.WeighingRecords.SingleAsync(Ct);
            db.Entry(record).Property("_actualValue").CurrentValue = 94.0m;
            await Should.ThrowAsync<InvalidOperationException>(() => db.SaveChangesAsync(Ct));
        }

        await using (var db = fixture.NewDbContext())
        {
            db.WeighingRecords.Remove(await db.WeighingRecords.SingleAsync(Ct));
            await Should.ThrowAsync<InvalidOperationException>(() => db.SaveChangesAsync(Ct));
        }

        await using var check = fixture.NewDbContext();
        (await check.WeighingRecords.SingleAsync(Ct)).Actual.Value.ShouldBe(94.2m);
    }

    [Fact]
    public async Task Commands_WithoutXsrf_Are400()
    {
        var response = await _client.Http.PostAsync(new Uri("/api/batches/B-24187/session/confirmation", UriKind.Relative), System.Net.Http.Json.JsonContent.Create(new { sequence = 3 }), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Migration_IsApplied()
    {
        await using var db = fixture.NewDbContext();
        (await db.Database.GetAppliedMigrationsAsync(Ct)).ShouldContain(m => m.EndsWith("_AddWeighingAttemptsAndRecords", StringComparison.Ordinal));
        typeof(WeighingRecord).ShouldNotBeNull();
    }
}
