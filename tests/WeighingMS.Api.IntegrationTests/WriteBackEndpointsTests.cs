using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeighingMS.Application.Batches;
using WeighingMS.Application.Weighing;
using WeighingMS.Application.WriteBack;
using WeighingMS.Domain.Audit;
using WeighingMS.Domain.Weighing;
using WeighingMS.Domain.WriteBack;

namespace WeighingMS.Api.IntegrationTests;

/// <summary>
/// Bolt 011 through the Api and the real dummy OrgaTEX: STEP 26 and the OrgaTEX-unavailable part of STEP 30. Tests drive
/// <see cref="WriteBackDispatcher"/> with the fake clock, except the two at the end, which run the real background loop.
/// </summary>
public sealed class WriteBackEndpointsTests(ApiFixture fixture) : IAsyncLifetime
{
    private const string _batch = "B-24187";
    private static readonly TimeSpan _pour = TimeSpan.FromSeconds(8);
    private static readonly (int Seq, decimal Target, string Unit)[] _teal4C =
        [(1, 1240.0m, "g"), (2, 386.0m, "g"), (3, 94.0m, "g"), (4, 24.000m, "kg"), (5, 9.600m, "kg")];

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

    private async Task HostAsync(IDictionary<string, string?>? settings = null, Action<IServiceCollection>? services = null)
    {
        _api = fixture.CreateApi(settings, _simulator, services);
        _client = new StationClient(_api);
        (await _client.SignInAsync()).EnsureSuccessStatusCode();
        (await Post("")).IsSuccessStatusCode.ShouldBeTrue();
    }

    private async Task RehostAsync(IDictionary<string, string?>? settings = null, Action<IServiceCollection>? services = null)
    {
        _client.Dispose();
        await _api.DisposeAsync();
        await HostAsync(settings, services);
    }

    private Task<HttpResponseMessage> Post(string path, object? body = null) =>
        _client.SendAsync(HttpMethod.Post, $"/api/batches/{_batch}/session{path}", body);

    private static async Task<JsonElement> Expect(Task<HttpResponseMessage> call, HttpStatusCode status)
    {
        var response = await call;
        response.StatusCode.ShouldBe(status, await response.Content.ReadAsStringAsync(Ct));
        return await response.ReadJsonAsync();
    }

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

    private async Task WeighAllAsync(int except = 0)
    {
        foreach (var (seq, target, unit) in _teal4C.Where(c => c.Seq != except))
        {
            await PourAsync(seq, "rising", target, unit);
            await Expect(Post("/confirmation", new { sequence = seq }), HttpStatusCode.Created);
        }
    }

    private async Task<DispatchResult> DispatchAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<WriteBackDispatcher>().DispatchDueAsync(Ct);
    }

    private async Task<JsonElement> ReceiptsAsync()
    {
        var response = await _simulator.Control.GetAsync(new Uri("/_sim/orgatex/consumption", UriKind.Relative), Ct);
        return await response.EnsureSuccessStatusCode().ReadJsonAsync();
    }

    private async Task<ConsumptionWriteBack> RowAsync()
    {
        await using var db = fixture.NewDbContext();
        return await db.WriteBacks.SingleAsync(Ct);
    }

    private async Task<JsonElement> OrgatexStateAsync() =>
        (await Expect(_client.GetAsync($"/api/batches/{_batch}/session/summary"), HttpStatusCode.OK)).GetProperty("orgatex");

    private async Task<string?> QueueStatusAsync()
    {
        var queue = await Expect(_client.GetAsync("/api/batches"), HttpStatusCode.OK);
        return queue.GetProperty("items").EnumerateArray().Single(b => b.GetProperty("batchId").GetString() == _batch).GetProperty("status").GetString();
    }

    private async Task<int> UnsyncedAsync() =>
        (await Expect(_client.GetAsync("/api/devices/health"), HttpStatusCode.OK)).GetProperty("unsyncedWriteBacks").GetInt32();

    private async Task AddRowAsync(string batchId, string unit = "g")
    {
        await using var db = fixture.NewDbContext();
        db.WriteBacks.Add(ConsumptionWriteBack.Create(batchId, Guid.CreateVersion7(), [new("DYE-4471", 94.2m, unit, "L-3")], fixture.Clock.GetUtcNow()));
        await db.SaveChangesAsync(Ct);
    }

    /// <summary>Runs five failing attempts on the configured 5, 10, 20, 40 s schedule.</summary>
    private async Task FailUntilSyncFailedAsync()
    {
        foreach (var wait in new[] { 5, 10, 20, 40 })
        {
            (await DispatchAsync()).Retrying.ShouldBe(1);
            fixture.Clock.Advance(TimeSpan.FromSeconds(wait));
        }

        (await DispatchAsync()).Failed.ShouldBe(1);
    }

    // ── Story 001: enqueue with completion ──────────────────────────────────────────────

    [Fact]
    public async Task Completion_AddsOnePendingRowWithEveryLine_AndDoesNotCallOrgatex()
    {
        await WeighAllAsync();

        var row = await RowAsync();
        row.BatchId.ShouldBe(_batch);
        row.Status.ShouldBe(WriteBackStatus.Pending);
        row.IdempotencyKey.ShouldNotBe(Guid.Empty);
        row.Lines.Select(l => (l.ChemicalId, l.Unit, l.Lot)).ShouldBe(
        [
            ("DYE-1180", "g", "L-1"),
            ("DYE-2204", "g", "L-2"),
            ("DYE-4471", "g", "L-3"),
            ("AUX-0101", "kg", "L-4"),
            ("AUX-0107", "kg", "L-5"),
        ]);
        row.Lines.Select(l => l.ActualQuantity).ShouldBe([1240.0m, 386.0m, 94.0m, 24.000m, 9.600m]);
        (await ReceiptsAsync()).GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Completion_AFailingHandler_LeavesNoRowAndNoCompletion()
    {
        await RehostAsync(services: s => s.AddScoped<IBatchCompletedHandler, FailingHandler>());
        await WeighAllAsync(except: 5);
        await PourAsync(5, "rising", 9.600m, "kg");

        (await Post("/confirmation", new { sequence = 5 })).StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        await using var db = fixture.NewDbContext();
        (await db.WriteBacks.CountAsync(Ct)).ShouldBe(0);
        (await db.WeighingSessions.SingleAsync(Ct)).Status.ShouldBe(WeighingSessionStatus.InProgress);
    }

    [Fact]
    public async Task Completion_WithOrgatexDown_StillSucceedsAndShowsPending()
    {
        await WeighAllAsync(except: 5);
        await PourAsync(5, "rising", 9.600m, "kg");
        await _simulator.FaultAsync("orgatex", "unavailable");

        await Expect(Post("/confirmation", new { sequence = 5 }), HttpStatusCode.Created);

        await using (var db = fixture.NewDbContext())
        {
            (await db.WeighingSessions.SingleAsync(Ct)).Status.ShouldBe(WeighingSessionStatus.Completed);
        }

        var state = await OrgatexStateAsync();
        state.GetProperty("state").GetString().ShouldBe("pending");
        state.GetProperty("attempts").GetInt32().ShouldBe(0);
        (await UnsyncedAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Completion_WithAnOverride_PostsTheActualOverweightQuantity()
    {
        await WeighAllAsync(except: 3);
        await PourAsync(3, "overshoot", 94.0m, "g", 0.5m);
        var grant = await Expect(_client.SendAsync(HttpMethod.Post, "/api/session/step-up", new { badgeId = "SV-2001", pin = "2468", action = "override" }), HttpStatusCode.OK);

        await Expect(Post("/override", new { sequence = 3, grantId = grant.GetProperty("grantId").GetGuid(), reason = "Shade lab accepts +2.4 g" }), HttpStatusCode.Created);
        (await DispatchAsync()).Sent.ShouldBe(1);

        var lines = (await ReceiptsAsync())[0].GetProperty("lines").EnumerateArray().ToList();
        lines.Count.ShouldBe(5);
        lines.Single(l => l.GetProperty("chemicalId").GetString() == "DYE-4471").GetProperty("actualQuantity").GetDecimal().ShouldBe(96.4m);
    }

    [Fact]
    public async Task Database_RefusesASecondRowForTheSameBatch()
    {
        await AddRowAsync(_batch);

        await Should.ThrowAsync<DbUpdateException>(() => AddRowAsync(_batch));
    }

    // ── Story 002: dispatch, idempotency, retry ─────────────────────────────────────────

    [Fact]
    public async Task Step26_Dispatch_StoresOneReceiptWithTheLinesAndKey_AndEveryScreenFieldFollows()
    {
        await WeighAllAsync();
        var key = (await RowAsync()).IdempotencyKey;
        (await QueueStatusAsync()).ShouldBe("syncPending");

        (await DispatchAsync()).ShouldBe(new DispatchResult(1, 0, 0));

        var receipts = await ReceiptsAsync();
        receipts.GetArrayLength().ShouldBe(1);
        receipts[0].GetProperty("batchId").GetString().ShouldBe(_batch);
        receipts[0].GetProperty("idempotencyKey").GetString().ShouldBe(key.ToString());
        receipts[0].GetProperty("lines").GetArrayLength().ShouldBe(5);

        var row = await RowAsync();
        row.Status.ShouldBe(WriteBackStatus.Sent);
        row.ReceivedAt.ShouldNotBeNull();

        var state = await OrgatexStateAsync();
        state.GetProperty("state").GetString().ShouldBe("sent");
        state.GetProperty("writtenAt").GetDateTimeOffset().ShouldBe(row.SentAt!.Value);
        state.GetProperty("errorCode").ValueKind.ShouldBe(JsonValueKind.Null);
        (await QueueStatusAsync()).ShouldBe("complete");
        (await UnsyncedAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Dispatch_Twice_PostsOnce()
    {
        await WeighAllAsync();

        (await DispatchAsync()).Sent.ShouldBe(1);
        (await DispatchAsync()).ShouldBe(new DispatchResult(0, 0, 0));

        (await ReceiptsAsync()).GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task RepostingTheSameKey_AsAfterACrashBeforeMarkingSent_StoresOneReceipt()
    {
        await WeighAllAsync();
        var row = await RowAsync();
        await using var scope = _api.Services.CreateAsyncScope();
        var gateway = scope.ServiceProvider.GetRequiredService<IOrgatexGateway>();

        var first = await gateway.PostConsumptionAsync(row.BatchId, row.IdempotencyKey, row.Lines, Ct);
        var second = await gateway.PostConsumptionAsync(row.BatchId, row.IdempotencyKey, row.Lines, Ct);

        second.Value.ShouldBe(first.Value);
        (await ReceiptsAsync()).GetArrayLength().ShouldBe(1);
    }

    [Theory]
    [InlineData("unavailable")]
    [InlineData("error500")]
    [InlineData("timeout")]
    public async Task Dispatch_TransientFault_StaysPendingWithTheNextAttemptScheduled(string mode)
    {
        await WeighAllAsync();
        await _simulator.FaultAsync("orgatex", mode);

        (await DispatchAsync()).ShouldBe(new DispatchResult(0, 1, 0));

        var row = await RowAsync();
        row.Status.ShouldBe(WriteBackStatus.Pending);
        row.Attempts.ShouldBe(1);
        row.NextAttemptAt.ShouldBe(fixture.Clock.GetUtcNow().AddSeconds(5));
        row.LastErrorCode.ShouldBe("OrgaTex.Unavailable");
        var state = await OrgatexStateAsync();
        state.GetProperty("attempts").GetInt32().ShouldBe(1);
        state.GetProperty("nextAttemptAt").GetDateTimeOffset().ShouldBe(row.NextAttemptAt!.Value);
    }

    [Fact]
    public async Task Dispatch_AfterAnOutageEndsBeforeTheLimit_PostsOnceWithTheSameKey()
    {
        await WeighAllAsync();
        var key = (await RowAsync()).IdempotencyKey;
        await _simulator.FaultAsync("orgatex", "unavailable");
        (await DispatchAsync()).Retrying.ShouldBe(1);
        await _simulator.ClearFaultsAsync();

        fixture.Clock.Advance(TimeSpan.FromSeconds(4));
        (await DispatchAsync()).ShouldBe(new DispatchResult(0, 0, 0)); // not due yet
        fixture.Clock.Advance(TimeSpan.FromSeconds(1));
        (await DispatchAsync()).Sent.ShouldBe(1);

        var receipts = await ReceiptsAsync();
        receipts.GetArrayLength().ShouldBe(1);
        receipts[0].GetProperty("idempotencyKey").GetString().ShouldBe(key.ToString());
    }

    // ── Story 003: sync failed and manual retry ─────────────────────────────────────────

    [Fact]
    public async Task Step30_OutageToTheLimit_IsSyncFailedAudited_AndRetryAfterRecoveryPostsOnce()
    {
        await WeighAllAsync();
        var key = (await RowAsync()).IdempotencyKey;
        await _simulator.FaultAsync("orgatex", "unavailable");

        await FailUntilSyncFailedAsync();

        var failed = await OrgatexStateAsync();
        failed.GetProperty("state").GetString().ShouldBe("syncFailed");
        failed.GetProperty("attempts").GetInt32().ShouldBe(5);
        failed.GetProperty("errorCode").GetString().ShouldBe("OrgaTex.Unavailable");
        (await UnsyncedAsync()).ShouldBe(1);
        await using (var db = fixture.NewDbContext())
        {
            (await db.AuditEntries.CountAsync(a => a.Type == AuditEventType.SyncFailed && a.Subject == _batch, Ct)).ShouldBe(1);
        }

        await _simulator.ClearFaultsAsync();
        (await QueueStatusAsync()).ShouldBe("syncFailed");
        (await DispatchAsync()).ShouldBe(new DispatchResult(0, 0, 0)); // nothing is sent until someone retries

        var retried = await Expect(Post("/write-back/retry"), HttpStatusCode.OK);
        retried.GetProperty("state").GetString().ShouldBe("pending");
        (await DispatchAsync()).Sent.ShouldBe(1);

        var receipts = await ReceiptsAsync();
        receipts.GetArrayLength().ShouldBe(1);
        receipts[0].GetProperty("idempotencyKey").GetString().ShouldBe(key.ToString());
        (await OrgatexStateAsync()).GetProperty("state").GetString().ShouldBe("sent");
        await using var after = fixture.NewDbContext();
        var audit = await after.AuditEntries.SingleAsync(a => a.Type == AuditEventType.WriteBackRetried, Ct);
        audit.ActorBadgeId.ShouldBe("OP-1001");
        audit.Subject.ShouldBe(_batch);
    }

    [Fact]
    public async Task Retry_DuringAContinuedOutage_RunsTheScheduleAgain()
    {
        await WeighAllAsync();
        await _simulator.FaultAsync("orgatex", "unavailable");
        await FailUntilSyncFailedAsync();

        await Expect(Post("/write-back/retry"), HttpStatusCode.OK);
        await FailUntilSyncFailedAsync();

        (await OrgatexStateAsync()).GetProperty("state").GetString().ShouldBe("syncFailed");
        (await ReceiptsAsync()).GetArrayLength().ShouldBe(0);
    }

    [Theory]
    [InlineData("B-NOPE", "g", "Batch.NotFound")] // 404
    [InlineData(_batch, "lb", "")] // 400 validation
    public async Task Dispatch_ARejection_IsSyncFailedAfterOneAttempt(string batchId, string unit, string expectedCode)
    {
        await AddRowAsync(batchId, unit);

        (await DispatchAsync()).ShouldBe(new DispatchResult(0, 0, 1));

        var row = await RowAsync();
        row.Status.ShouldBe(WriteBackStatus.SyncFailed);
        row.Attempts.ShouldBe(1);
        row.LastErrorCode.ShouldNotBeNull().ShouldStartWith($"OrgaTex.ConsumptionRejected:{expectedCode}");
    }

    [Fact]
    public async Task Retry_WhilePending_Is409()
    {
        await WeighAllAsync();

        var response = await Post("/write-back/retry");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.ErrorCodeAsync()).ShouldBe("WriteBack.NotFailed");
    }

    [Fact]
    public async Task Retry_AfterSent_Is409AndPostsNothingMore()
    {
        await WeighAllAsync();
        await DispatchAsync();

        (await Post("/write-back/retry")).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        (await DispatchAsync()).Sent.ShouldBe(0);
        (await ReceiptsAsync()).GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task Retry_BeforeCompletion_Is404()
    {
        var response = await Post("/write-back/retry");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.ErrorCodeAsync()).ShouldBe("WriteBack.NotFound");
    }

    [Fact]
    public async Task Retry_ByAnotherOperator_IsAllowed()
    {
        await AddRowAsync("B-NOPE");
        await DispatchAsync();
        await using var api = fixture.CreateApi(simulator: _simulator);
        using var other = new StationClient(api);
        (await other.SignInAsync("OP-1002")).EnsureSuccessStatusCode();

        var response = await other.SendAsync(HttpMethod.Post, "/api/batches/B-NOPE/session/write-back/retry");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await using var db = fixture.NewDbContext();
        (await db.AuditEntries.SingleAsync(a => a.Type == AuditEventType.WriteBackRetried, Ct)).ActorBadgeId.ShouldBe("OP-1002");
    }

    [Fact]
    public async Task Retry_NotSignedIn_Is401()
    {
        await using var api = fixture.CreateApi(simulator: _simulator);
        using var anonymous = new StationClient(api);
        await anonymous.PrimeAntiforgeryAsync();

        (await anonymous.SendAsync(HttpMethod.Post, $"/api/batches/{_batch}/session/write-back/retry")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── Story 004: visibility ───────────────────────────────────────────────────────────

    [Fact]
    public async Task State_BeforeCompletion_IsNotAvailable()
    {
        var state = await Expect(_client.GetAsync($"/api/batches/{_batch}/session/write-back"), HttpStatusCode.OK);

        state.GetProperty("state").GetString().ShouldBe("notAvailable");
        (await UnsyncedAsync()).ShouldBe(0);
        (await QueueStatusAsync()).ShouldBe("weighing");
    }

    [Fact]
    public async Task State_SurvivesAnApiRestart_AndTheRowIsPostedAfterwards()
    {
        await WeighAllAsync();
        var key = (await RowAsync()).IdempotencyKey;

        await RehostAsync();

        (await OrgatexStateAsync()).GetProperty("state").GetString().ShouldBe("pending");
        (await DispatchAsync()).Sent.ShouldBe(1);
        (await ReceiptsAsync())[0].GetProperty("idempotencyKey").GetString().ShouldBe(key.ToString());
    }

    // ── The real background loop ────────────────────────────────────────────────────────

    private static readonly Dictionary<string, string?> _loopOn = new()
    {
        ["WriteBack:Enabled"] = "true",
        ["WriteBack:PollInterval"] = "00:00:00.100",
    };

    private async Task<bool> WaitUntilSentAsync()
    {
        for (var i = 0; i < 100; i++)
        {
            if ((await RowAsync()).Status == WriteBackStatus.Sent)
            {
                return true;
            }

            await Task.Delay(100, Ct);
        }

        return false;
    }

    [Fact]
    public async Task BackgroundLoop_DeliversACompletionWithoutAnyoneAsking()
    {
        await RehostAsync(_loopOn);

        await WeighAllAsync();

        (await WaitUntilSentAsync()).ShouldBeTrue();
        (await ReceiptsAsync()).GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task BackgroundLoop_ResumesAPendingRowAfterARestart()
    {
        await WeighAllAsync(); // the loop is off in this host, so the row stays pending
        (await RowAsync()).Status.ShouldBe(WriteBackStatus.Pending);

        await RehostAsync(_loopOn);

        (await WaitUntilSentAsync()).ShouldBeTrue();
        (await ReceiptsAsync()).GetArrayLength().ShouldBe(1);
    }

    private sealed class FailingHandler : IBatchCompletedHandler
    {
        public Task HandleAsync(BatchCompletedNotice notice, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("handler failed");
    }
}
