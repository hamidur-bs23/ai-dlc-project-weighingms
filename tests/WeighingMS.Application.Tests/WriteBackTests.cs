using ErrorOr;
using Microsoft.Extensions.Time.Testing;
using WeighingMS.Application.Batches;
using WeighingMS.Application.WriteBack;
using WeighingMS.Domain.Audit;
using WeighingMS.Domain.WriteBack;

namespace WeighingMS.Application.Tests;

/// <summary>The dispatcher's outcomes, the status view, the unsynced count and manual retry (bolt 011).</summary>
public sealed class WriteBackTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 6, 3, 40, 0, TimeSpan.Zero));
    private readonly FakeWriteBacks _writeBacks = new();
    private readonly ScriptedOrgatex _orgatex = new();
    private readonly FakeAudit _audit = new();
    private readonly FakeSignal _signal = new();
    private readonly WriteBackDispatcher _dispatcher;
    private readonly WriteBackService _service;

    public WriteBackTests()
    {
        var clock = new FakePlantClock(_time);
        var options = new WriteBackOptions { MaxAttempts = 3, BaseDelay = TimeSpan.FromSeconds(5), MaxDelay = TimeSpan.FromMinutes(5), PollInterval = TimeSpan.FromSeconds(2) };
        _dispatcher = new WriteBackDispatcher(_writeBacks, _orgatex, options, _audit, clock);
        _service = new WriteBackService(_writeBacks, _signal, _audit, clock);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private ConsumptionWriteBack Stored(string batchId = "B-24187")
    {
        var writeBack = ConsumptionWriteBack.Create(batchId, Guid.CreateVersion7(), [new("DYE-4471", 94.2m, "g", "2026-0387")], _time.GetUtcNow());
        _writeBacks.Add(writeBack);
        return writeBack;
    }

    [Fact]
    public async Task Dispatch_A200_MarksItSentWithTheReceiptTimeAndKey()
    {
        var writeBack = Stored();
        var received = _time.GetUtcNow().AddSeconds(-1);
        _orgatex.Outcomes.Enqueue(received);

        var result = await _dispatcher.DispatchDueAsync(Ct);

        result.ShouldBe(new DispatchResult(1, 0, 0));
        writeBack.Status.ShouldBe(WriteBackStatus.Sent);
        writeBack.ReceivedAt.ShouldBe(received);
        _orgatex.Posts.ShouldBe([("B-24187", writeBack.IdempotencyKey)]);
        _writeBacks.Saves.ShouldBe(1);
        _audit.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Dispatch_Transient_RetriesWithBackOffAndOnlyWhenDue()
    {
        var writeBack = Stored();
        _orgatex.Outcomes.Enqueue(OrgatexErrors.Unavailable);

        var first = await _dispatcher.DispatchDueAsync(Ct);
        var tooEarly = await _dispatcher.DispatchDueAsync(Ct);

        first.ShouldBe(new DispatchResult(0, 1, 0));
        tooEarly.ShouldBe(new DispatchResult(0, 0, 0));
        _orgatex.Posts.Count.ShouldBe(1);
        writeBack.NextAttemptAt.ShouldBe(_time.GetUtcNow().AddSeconds(5));
        writeBack.LastErrorCode.ShouldBe("OrgaTex.Unavailable");

        _time.Advance(TimeSpan.FromSeconds(5));
        _orgatex.Outcomes.Enqueue(_time.GetUtcNow());
        (await _dispatcher.DispatchDueAsync(Ct)).Sent.ShouldBe(1);
        _orgatex.Posts.Count.ShouldBe(2);
        _orgatex.Posts.Select(p => p.Key).Distinct().Count().ShouldBe(1);
    }

    [Fact]
    public async Task Dispatch_LimitReached_IsSyncFailedAndAuditedOnce()
    {
        var writeBack = Stored();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            _orgatex.Outcomes.Enqueue(OrgatexErrors.Unavailable);
            await _dispatcher.DispatchDueAsync(Ct);
            _time.Advance(TimeSpan.FromMinutes(1));
        }

        (await _dispatcher.DispatchDueAsync(Ct)).ShouldBe(new DispatchResult(0, 0, 0));

        writeBack.Status.ShouldBe(WriteBackStatus.SyncFailed);
        _orgatex.Posts.Count.ShouldBe(3);
        var entry = _audit.Entries.ShouldHaveSingleItem();
        entry.Type.ShouldBe(AuditEventType.SyncFailed);
        entry.Subject.ShouldBe("B-24187");
        entry.ActorBadgeId.ShouldBe("system");
        entry.Reason!.ShouldContain("3 attempt(s)");
    }

    [Theory]
    [InlineData("Consumption.InvalidPayload", 400)]
    [InlineData("Batch.NotFound", 404)]
    [InlineData("Consumption.IdempotencyKeyConflict", 409)]
    public async Task Dispatch_Rejection_IsSyncFailedAtOnceWithTheOrgatexCode(string code, int status)
    {
        var writeBack = Stored();
        _orgatex.Outcomes.Enqueue(OrgatexErrors.ConsumptionRejected(code, status));

        var result = await _dispatcher.DispatchDueAsync(Ct);

        result.ShouldBe(new DispatchResult(0, 0, 1));
        writeBack.Status.ShouldBe(WriteBackStatus.SyncFailed);
        writeBack.Attempts.ShouldBe(1);
        writeBack.LastErrorCode.ShouldBe($"OrgaTex.ConsumptionRejected:{code}");
        _audit.Entries.ShouldHaveSingleItem().Type.ShouldBe(AuditEventType.SyncFailed);
    }

    [Fact]
    public async Task Dispatch_OneFailureDoesNotStopTheNextWriteBack()
    {
        var failing = Stored("B-24187");
        var healthy = Stored("B-24188");
        _orgatex.Outcomes.Enqueue(OrgatexErrors.Unavailable);
        _orgatex.Outcomes.Enqueue(_time.GetUtcNow());

        var result = await _dispatcher.DispatchDueAsync(Ct);

        result.ShouldBe(new DispatchResult(1, 1, 0));
        failing.Status.ShouldBe(WriteBackStatus.Pending);
        healthy.Status.ShouldBe(WriteBackStatus.Sent);
    }

    [Fact]
    public async Task Dispatch_LeavesSentAndFailedOnesAlone()
    {
        Stored("B-24187").RecordSent(_time.GetUtcNow(), _time.GetUtcNow());
        Stored("B-24188").RecordFailure("x", retryable: false, new RetryPolicy(1, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)), _time.GetUtcNow());

        (await _dispatcher.DispatchDueAsync(Ct)).ShouldBe(new DispatchResult(0, 0, 0));

        _orgatex.Posts.ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_WithoutAWriteBack_IsNull() =>
        (await _service.GetAsync("B-24187", Ct)).ShouldBeNull();

    [Fact]
    public async Task Get_ShowsStatusAttemptsAndNextAttempt()
    {
        Stored().RecordFailure("OrgaTex.Unavailable", retryable: true, new RetryPolicy(5, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5)), _time.GetUtcNow());

        var view = await _service.GetAsync("B-24187", Ct);

        view.ShouldBe(new WriteBackView(WriteBackStatus.Pending, 1, _time.GetUtcNow().AddSeconds(5), null, "OrgaTex.Unavailable"));
    }

    [Fact]
    public async Task CountUnsynced_CountsPendingAndFailedButNotSent()
    {
        Stored("B-24187");
        Stored("B-24188").RecordFailure("x", retryable: false, new RetryPolicy(1, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)), _time.GetUtcNow());
        Stored("B-24190").RecordSent(_time.GetUtcNow(), _time.GetUtcNow());

        (await _service.CountUnsyncedAsync(Ct)).ShouldBe(2);
    }

    [Fact]
    public async Task Retry_FromSyncFailed_IsPendingAuditedAndWakesTheDispatcher()
    {
        var writeBack = Stored();
        writeBack.RecordFailure("x", retryable: false, new RetryPolicy(1, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)), _time.GetUtcNow());

        var result = await _service.RetryAsync("B-24187", "OP-1002", Ct);

        result.Value.Status.ShouldBe(WriteBackStatus.Pending);
        var entry = _audit.Entries.ShouldHaveSingleItem();
        entry.Type.ShouldBe(AuditEventType.WriteBackRetried);
        entry.ActorBadgeId.ShouldBe("OP-1002");
        entry.Subject.ShouldBe("B-24187");
        _signal.Notifications.ShouldBe(1);
    }

    [Fact]
    public async Task Retry_WithoutAWriteBack_IsNotFound()
    {
        var result = await _service.RetryAsync("B-24187", "OP-1001", Ct);

        result.FirstError.Code.ShouldBe("WriteBack.NotFound");
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Retry_WhilePending_IsRefusedWithoutAuditOrWakeUp()
    {
        Stored();

        var result = await _service.RetryAsync("B-24187", "OP-1001", Ct);

        result.FirstError.Code.ShouldBe("WriteBack.NotFailed");
        _audit.Entries.ShouldBeEmpty();
        _signal.Notifications.ShouldBe(0);
    }

    [Fact]
    public async Task Retry_LosingARaceToTheDispatcher_IsAConflictWithoutAWakeUp()
    {
        Stored().RecordFailure("x", retryable: false, new RetryPolicy(1, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)), _time.GetUtcNow());
        _writeBacks.Conflict = true;

        var result = await _service.RetryAsync("B-24187", "OP-1001", Ct);

        result.FirstError.Code.ShouldBe("Concurrency.Conflict");
        _signal.Notifications.ShouldBe(0);
    }

    [Theory]
    [InlineData(5, 5, 300, 2, true)]
    [InlineData(0, 5, 300, 2, false)]
    [InlineData(5, 0, 300, 2, false)]
    [InlineData(5, 10, 5, 2, false)]
    [InlineData(5, 5, 300, 0, false)]
    public void Options_AreValidated(int attempts, int baseSeconds, int maxSeconds, int pollSeconds, bool valid)
    {
        var options = new WriteBackOptions
        {
            MaxAttempts = attempts,
            BaseDelay = TimeSpan.FromSeconds(baseSeconds),
            MaxDelay = TimeSpan.FromSeconds(maxSeconds),
            PollInterval = TimeSpan.FromSeconds(pollSeconds),
        };

        (options.Validate().Count == 0).ShouldBe(valid);
    }

    /// <summary>A gateway whose consumption posts answer from a script; it records what was posted.</summary>
    private sealed class ScriptedOrgatex : IOrgatexGateway
    {
        public Queue<ErrorOr<DateTimeOffset>> Outcomes { get; } = new();

        public List<(string BatchId, Guid Key)> Posts { get; } = [];

        public Task<ErrorOr<DateTimeOffset>> PostConsumptionAsync(string batchId, Guid idempotencyKey, IReadOnlyList<ConsumptionLine> lines, CancellationToken cancellationToken)
        {
            Posts.Add((batchId, idempotencyKey));
            return Task.FromResult(Outcomes.Dequeue());
        }

        public Task<ErrorOr<IReadOnlyList<OrgatexBatch>>> GetBatchesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ErrorOr<OrgatexBatch>> GetBatchAsync(string batchId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ErrorOr<OrgatexRecipe>> GetRecipeAsync(string batchId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
