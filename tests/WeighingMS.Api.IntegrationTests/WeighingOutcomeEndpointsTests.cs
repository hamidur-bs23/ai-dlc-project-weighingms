using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeighingMS.Application.Abstractions;
using WeighingMS.Application.Weighing;
using WeighingMS.Domain.Audit;
using WeighingMS.Domain.Weighing;

namespace WeighingMS.Api.IntegrationTests;

/// <summary>Bolt 009 through the Api: STEP 24 overshoot, override, completion in one transaction, summary, review.</summary>
public sealed class WeighingOutcomeEndpointsTests(ApiFixture fixture) : IAsyncLifetime
{
    private static readonly TimeSpan _pour = TimeSpan.FromSeconds(8);
    private readonly SimulatorHost _simulator = new();
    private WebApplicationFactory<Program> _api = null!;
    private StationClient _client = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        await HostAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _api.DisposeAsync();
        await _simulator.DisposeAsync();
    }

    private async Task HostAsync(string badge = "OP-1001", Action<IServiceCollection>? services = null)
    {
        _api = fixture.CreateApi(simulator: _simulator, services: services);
        _client = new StationClient(_api);
        (await _client.SignInAsync(badge)).EnsureSuccessStatusCode();
        var open = await Post("");
        open.IsSuccessStatusCode.ShouldBeTrue();
    }

    private async Task RehostAsync(string badge = "OP-1001", Action<IServiceCollection>? services = null)
    {
        _client.Dispose();
        await _api.DisposeAsync();
        await HostAsync(badge, services);
    }

    private Task<HttpResponseMessage> Post(string path, object? body = null) =>
        _client.SendAsync(HttpMethod.Post, $"/api/batches/B-24187/session{path}", body);

    private static async Task<JsonElement> Expect(Task<HttpResponseMessage> call, HttpStatusCode status)
    {
        var response = await call;
        response.StatusCode.ShouldBe(status, await response.Content.ReadAsStringAsync(Ct));
        return await response.ReadJsonAsync();
    }

    private static decimal Value(JsonElement q) => q.GetProperty("value").GetDecimal();

    private async Task PourAsync(int sequence, string scenario, decimal target, string unit, decimal? tolerance = null)
    {
        var scaleId = unit == "g" ? "scale-3kg" : "scale-32kg";
        await _simulator.PostAsync("/_sim/reset", new { });
        await Expect(Post("/selection", new { sequence }), HttpStatusCode.OK);
        await Expect(Post("/attempt/lot", new { sequence, lot = $"L-{sequence}" }), HttpStatusCode.OK);
        await Expect(Post("/attempt/container", new { sequence, container = $"C-{sequence}" }), HttpStatusCode.OK);
        await Expect(Post("/tare", new { sequence }), HttpStatusCode.OK);
        await _simulator.PostAsync($"/_sim/scales/{scaleId}/scenario", new { scenario, target = new { value = target, unit }, tolerance });
        _simulator.Clock.Advance(_pour);
    }

    private async Task<Guid> GrantAsync(string supervisor = "SV-2001", string pin = "2468")
    {
        var grant = await Expect(_client.SendAsync(HttpMethod.Post, "/api/session/step-up", new { badgeId = supervisor, pin, action = "override" }), HttpStatusCode.OK);
        return grant.GetProperty("grantId").GetGuid();
    }

    private static readonly (int Seq, decimal Target, string Unit)[] _teal4C =
        [(1, 1240.0m, "g"), (2, 386.0m, "g"), (3, 94.0m, "g"), (4, 24.000m, "kg"), (5, 9.600m, "kg")];

    private async Task<JsonElement> WeighAllAsync(int except = 0)
    {
        JsonElement last = default;
        foreach (var (seq, target, unit) in _teal4C.Where(c => c.Seq != except))
        {
            await PourAsync(seq, "rising", target, unit);
            last = await Expect(Post("/confirmation", new { sequence = seq }), HttpStatusCode.Created);
        }

        return last;
    }

    [Fact]
    public async Task Step24_Overshoot_IsBlockedWithTheSurplusAndOverrideOffered()
    {
        await PourAsync(3, "overshoot", 94.0m, "g", 0.5m);

        var reading = await Expect(_client.GetAsync("/api/batches/B-24187/session/reading"), HttpStatusCode.OK);
        var confirm = await Expect(Post("/confirmation", new { sequence = 3 }), HttpStatusCode.UnprocessableEntity);

        reading.GetProperty("class").GetString().ShouldBe("overweight");
        Value(reading.GetProperty("surplus").GetProperty("amount")).ShouldBe(2.4m);
        reading.GetProperty("surplus").GetProperty("percentOfTarget").GetDecimal().ShouldBe(2.6m);
        reading.GetProperty("override").GetProperty("offered").GetBoolean().ShouldBeTrue();
        reading.GetProperty("confirmation").GetProperty("canConfirm").GetBoolean().ShouldBeFalse();
        confirm.GetProperty("errorCode").GetString().ShouldBe("Weighing.OutOfTolerance");
    }

    [Fact]
    public async Task Step24_Override_RecordsOverweightSurplusAndOvrAndUsesTheGrantOnce()
    {
        await PourAsync(3, "overshoot", 94.0m, "g", 0.5m);
        var grant = await GrantAsync();

        var body = await Expect(Post("/override", new { sequence = 3, grantId = grant, reason = "Shade lab accepts +2.4 g" }), HttpStatusCode.Created);
        var reused = await Post("/override", new { sequence = 4, grantId = grant, reason = "again" });

        var record = body.GetProperty("record");
        record.GetProperty("class").GetString().ShouldBe("overweight");
        Value(record.GetProperty("actual")).ShouldBe(96.4m);
        Value(record.GetProperty("surplus").GetProperty("amount")).ShouldBe(2.4m);
        record.GetProperty("override").GetProperty("supervisorBadgeId").GetString().ShouldBe("SV-2001");
        record.GetProperty("override").GetProperty("supervisorName").GetString().ShouldBe("M. Rahman");
        record.GetProperty("override").GetProperty("reason").GetString().ShouldBe("Shade lab accepts +2.4 g");
        body.GetProperty("session").GetProperty("components")[2].GetProperty("record").GetProperty("override").GetBoolean().ShouldBeTrue();
        reused.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await reused.ErrorCodeAsync()).ShouldBe("Auth.GrantInvalid");

        await using var db = fixture.NewDbContext();
        var stored = await db.WeighingRecords.SingleAsync(Ct);
        stored.IsOverride.ShouldBeTrue();
        stored.Override!.GrantId.ShouldBe(grant);
        (await db.StepUpGrants.SingleAsync(g => g.Id == grant, Ct)).UsedAt.ShouldNotBeNull();
        (await db.AuditEntries.CountAsync(a => a.Type == AuditEventType.Override && a.Subject == "B-24187#3", Ct)).ShouldBe(1);
    }

    [Fact]
    public async Task Step24_OtherTargetsAreUnchangedAfterAnOvershootAndOverride()
    {
        var before = await Expect(_client.GetAsync("/api/batches/B-24187/session"), HttpStatusCode.OK);
        await PourAsync(3, "overshoot", 94.0m, "g", 0.5m);
        await Expect(Post("/override", new { sequence = 3, grantId = await GrantAsync(), reason = "ok" }), HttpStatusCode.Created);

        var after = await Expect(_client.GetAsync("/api/batches/B-24187/session"), HttpStatusCode.OK);

        string Targets(JsonElement s) => string.Join(";", s.GetProperty("components").EnumerateArray().Select(c => $"{c.GetProperty("target")}|{c.GetProperty("tolerance")}"));
        Targets(after).ShouldBe(Targets(before));
    }

    [Fact]
    public async Task Override_FixableRejection_LeavesTheGrantForTheNextTry()
    {
        await PourAsync(3, "overshoot", 94.0m, "g", 0.5m);
        var grant = await GrantAsync();

        var noReason = await Expect(Post("/override", new { sequence = 3, grantId = grant, reason = " " }), HttpStatusCode.UnprocessableEntity);
        var retry = await Post("/override", new { sequence = 3, grantId = grant, reason = "Accepted after review" });

        noReason.GetProperty("errors").EnumerateObject().Select(p => p.Name).ShouldBe(["reason"]);
        retry.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Override_InRange_Is409UseConfirm()
    {
        await PourAsync(3, "enterRange", 94.0m, "g", 0.5m);

        var response = await Post("/override", new { sequence = 3, grantId = await GrantAsync(), reason = "x" });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.ErrorCodeAsync()).ShouldBe("Weighing.OverrideNotNeeded");
    }

    [Fact]
    public async Task Override_SupervisorSignedInAsTheOperator_Is403FourEyes()
    {
        await RehostAsync("SV-2001");
        await PourAsync(3, "overshoot", 94.0m, "g", 0.5m);

        var response = await Post("/override", new { sequence = 3, grantId = await GrantAsync(), reason = "self" });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await response.ErrorCodeAsync()).ShouldBe("Auth.FourEyesRequired");
    }

    [Fact]
    public async Task Override_WrongPin_NoGrantAndNothingChanges()
    {
        await PourAsync(3, "overshoot", 94.0m, "g", 0.5m);

        var stepUp = await _client.SendAsync(HttpMethod.Post, "/api/session/step-up", new { badgeId = "SV-2001", pin = "0000", action = "override" });

        stepUp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        await using var db = fixture.NewDbContext();
        (await db.WeighingRecords.CountAsync(Ct)).ShouldBe(0);
    }

    [Fact]
    public async Task Override_SixConcurrentRequestsWithOneGrant_WriteOneRecord()
    {
        await PourAsync(3, "overshoot", 94.0m, "g", 0.5m);
        var grant = await GrantAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Post("/override", new { sequence = 3, grantId = grant, reason = "ok" })));

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        responses.Count(r => r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.Forbidden).ShouldBe(5);
        await using var db = fixture.NewDbContext();
        (await db.WeighingRecords.CountAsync(Ct)).ShouldBe(1);
    }

    [Fact]
    public async Task Override_WithoutGrantId_Is422()
    {
        var response = await Post("/override", new { sequence = 3, reason = "x" });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Completion_WhilePending_Is409AndTheLastConfirmCompletesAutomatically()
    {
        var early = await Post("/completion");
        early.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await early.ErrorCodeAsync()).ShouldBe("Batch.Incomplete");

        var last = await WeighAllAsync();

        var session = last.GetProperty("session");
        session.GetProperty("status").GetString().ShouldBe("completed");
        session.GetProperty("completedBy").GetString().ShouldBe("OP-1001");
        session.GetProperty("activeSequence").ValueKind.ShouldBe(JsonValueKind.Null);
        (await Expect(Post("/completion"), HttpStatusCode.OK)).GetProperty("status").GetString().ShouldBe("completed");
        await using var db = fixture.NewDbContext();
        (await db.AuditEntries.CountAsync(a => a.Type == AuditEventType.BatchCompleted && a.Subject == "B-24187", Ct)).ShouldBe(1);
    }

    [Fact]
    public async Task Completion_HandlersRunInsideTheCompletingTransaction()
    {
        await RehostAsync(services: s => s.AddScoped<IBatchCompletedHandler, AuditingHandler>());

        await WeighAllAsync();

        await using var db = fixture.NewDbContext();
        (await db.AuditEntries.CountAsync(a => a.Subject == "handler:B-24187:SO-9931", Ct)).ShouldBe(1);
    }

    [Fact]
    public async Task Completion_AFailingHandler_RollsBackTheLastRecordAndTheCompletion()
    {
        await RehostAsync(services: s => s.AddScoped<IBatchCompletedHandler, FailingHandler>());
        await WeighAllAsync(except: 5);
        await PourAsync(5, "rising", 9.600m, "kg");

        var last = await Post("/confirmation", new { sequence = 5 });

        last.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        await using var db = fixture.NewDbContext();
        (await db.WeighingRecords.CountAsync(Ct)).ShouldBe(4);
        (await db.WeighingSessions.SingleAsync(Ct)).Status.ShouldBe(WeighingSessionStatus.InProgress);
        (await db.AuditEntries.CountAsync(a => a.Type == AuditEventType.BatchCompleted, Ct)).ShouldBe(0);
    }

    [Fact]
    public async Task CompletedBatch_IsFrozen()
    {
        await WeighAllAsync();

        var select = await Post("/selection", new { sequence = 1 });
        var tare = await Post("/tare", new { sequence = 1 });

        select.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await select.ErrorCodeAsync()).ShouldBe("Weighing.BatchCompleted");
        tare.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await Expect(Post(""), HttpStatusCode.OK)).GetProperty("status").GetString().ShouldBe("completed");
    }

    [Fact]
    public async Task Summary_AfterCompletion_ShowsViewThreeAndSurvivesARestart()
    {
        await WeighAllAsync();
        await RehostAsync("OP-1002");

        var summary = await Expect(_client.GetAsync("/api/batches/B-24187/session/summary"), HttpStatusCode.OK);

        summary.GetProperty("status").GetString().ShouldBe("completed");
        summary.GetProperty("done").GetInt32().ShouldBe(5);
        summary.GetProperty("total").GetInt32().ShouldBe(5);
        summary.GetProperty("allWithinTolerance").GetBoolean().ShouldBeTrue();
        summary.GetProperty("overrideCount").GetInt32().ShouldBe(0);
        Value(summary.GetProperty("totalDispensed")).ShouldBe(35.320m); // 1240.0 + 386.0 + 94.0 g + 24.000 + 9.600 kg
        summary.GetProperty("totalDispensed").GetProperty("unit").GetString().ShouldBe("kg");
        summary.GetProperty("operators").EnumerateArray().Select(o => o.GetString()).ShouldBe(["R. Karim"]);
        summary.GetProperty("orgatex").GetProperty("state").GetString().ShouldBe("notAvailable");
        summary.GetProperty("ticket").GetProperty("state").GetString().ShouldBe("notAvailable");
        (summary.GetProperty("orderId").GetString(), summary.GetProperty("machine").GetString(), summary.GetProperty("shade").GetString()).ShouldBe(("SO-9931", "JET-04", "Teal 4C"));
        var lines = summary.GetProperty("lines").EnumerateArray().ToList();
        lines.Select(l => l.GetProperty("status").GetString()).ShouldAllBe(s => s == "targetAchieved");
        summary.GetProperty("elapsedSeconds").GetInt64().ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ReviewList_ShowsCompletedBatchesNewestFirst()
    {
        var empty = await Expect(_client.GetAsync("/api/weighing-sessions?status=completed"), HttpStatusCode.OK);
        empty.GetProperty("items").GetArrayLength().ShouldBe(0);

        await PourAsync(3, "overshoot", 94.0m, "g", 0.5m);
        await Expect(Post("/override", new { sequence = 3, grantId = await GrantAsync(), reason = "ok" }), HttpStatusCode.Created);
        await WeighAllAsync(except: 3);

        var list = await Expect(_client.GetAsync("/api/weighing-sessions?status=completed&page=1"), HttpStatusCode.OK);
        var bad = await _client.GetAsync("/api/weighing-sessions?status=open");

        list.GetProperty("total").GetInt32().ShouldBe(1);
        list.GetProperty("pageSize").GetInt32().ShouldBe(20);
        var item = list.GetProperty("items")[0];
        item.GetProperty("batchId").GetString().ShouldBe("B-24187");
        item.GetProperty("allWithinTolerance").GetBoolean().ShouldBeFalse();
        item.GetProperty("overrideCount").GetInt32().ShouldBe(1);
        bad.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Migration_IsApplied()
    {
        await using var db = fixture.NewDbContext();
        (await db.Database.GetAppliedMigrationsAsync(Ct)).ShouldContain(m => m.EndsWith("_AddOverridesAndCompletion", StringComparison.Ordinal));
    }

    /// <summary>Writes an audit entry: proves a handler's work is saved with the completion.</summary>
    private sealed class AuditingHandler(IAuditTrail audit) : IBatchCompletedHandler
    {
        public Task HandleAsync(BatchCompletedNotice notice, CancellationToken cancellationToken)
        {
            audit.Append(AuditEntry.Record(AuditEventType.BatchCompleted, notice.Completed.OperatorBadgeId, notice.Completed.OccurredAt, $"handler:{notice.Completed.BatchId}:{notice.OrderId}"));
            return Task.CompletedTask;
        }
    }

    /// <summary>Fails: the whole completion (last record included) must roll back.</summary>
    private sealed class FailingHandler : IBatchCompletedHandler
    {
        public Task HandleAsync(BatchCompletedNotice notice, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Downstream failed during completion.");
    }
}
