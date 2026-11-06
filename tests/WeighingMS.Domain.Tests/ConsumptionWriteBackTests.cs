using WeighingMS.Domain.WriteBack;

namespace WeighingMS.Domain.Tests;

/// <summary>The write-back state machine and retry schedule (bolt 011, FR-20, PD-01).</summary>
public sealed class ConsumptionWriteBackTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 6, 3, 40, 0, TimeSpan.Zero);
    private static readonly RetryPolicy _policy = new(5, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));
    private static readonly ConsumptionLine[] _lines = [new("DYE-4471", 94.2m, "g", "2026-0387")];

    private static ConsumptionWriteBack New() => ConsumptionWriteBack.Create("B-24187", Guid.CreateVersion7(), _lines, _now);

    [Fact]
    public void Create_IsPendingAndDueImmediately()
    {
        var writeBack = New();

        writeBack.Status.ShouldBe(WriteBackStatus.Pending);
        writeBack.Attempts.ShouldBe(0);
        writeBack.IsDue(_now).ShouldBeTrue();
        writeBack.IdempotencyKey.ShouldNotBe(Guid.Empty);
        writeBack.Lines.ShouldBe(_lines);
        writeBack.SentAt.ShouldBeNull();
    }

    [Fact]
    public void Create_WithoutLines_Throws() =>
        Should.Throw<ArgumentException>(() => ConsumptionWriteBack.Create("B-24187", Guid.CreateVersion7(), [], _now));

    [Fact]
    public void Create_GivesEachWriteBackItsOwnKey() =>
        New().IdempotencyKey.ShouldNotBe(New().IdempotencyKey);

    [Fact]
    public void RecordSent_StoresTheReceiptAndIsNoLongerDue()
    {
        var writeBack = New();
        var received = _now.AddSeconds(1);

        writeBack.RecordSent(received, _now.AddSeconds(2));

        writeBack.Status.ShouldBe(WriteBackStatus.Sent);
        writeBack.Attempts.ShouldBe(1);
        writeBack.ReceivedAt.ShouldBe(received);
        writeBack.SentAt.ShouldBe(_now.AddSeconds(2));
        writeBack.NextAttemptAt.ShouldBeNull();
        writeBack.IsDue(_now.AddDays(1)).ShouldBeFalse();
    }

    [Fact]
    public void RecordSent_AfterAFailure_ClearsTheErrorCode()
    {
        var writeBack = New();
        writeBack.RecordFailure("OrgaTex.Unavailable", retryable: true, _policy, _now);

        writeBack.RecordSent(_now, _now);

        writeBack.LastErrorCode.ShouldBeNull();
    }

    [Fact]
    public void RecordFailure_Transient_StaysPendingUntilTheBackOffHasPassed()
    {
        var writeBack = New();

        var failed = writeBack.RecordFailure("OrgaTex.Unavailable", retryable: true, _policy, _now);

        failed.ShouldBeFalse();
        writeBack.Status.ShouldBe(WriteBackStatus.Pending);
        writeBack.LastErrorCode.ShouldBe("OrgaTex.Unavailable");
        writeBack.IsDue(_now.AddSeconds(4.999)).ShouldBeFalse();
        writeBack.IsDue(_now.AddSeconds(5)).ShouldBeTrue();
    }

    [Fact]
    public void RecordFailure_FiveTransientFailures_FollowTheScheduleThenSyncFailed()
    {
        var writeBack = New();
        var waits = new List<TimeSpan>();
        var now = _now;

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            writeBack.RecordFailure("OrgaTex.Unavailable", retryable: true, _policy, now).ShouldBeFalse();
            waits.Add(writeBack.NextAttemptAt!.Value - now);
            now = writeBack.NextAttemptAt.Value;
        }

        var failed = writeBack.RecordFailure("OrgaTex.Unavailable", retryable: true, _policy, now);

        waits.ShouldBe([TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(40)]);
        failed.ShouldBeTrue();
        writeBack.Status.ShouldBe(WriteBackStatus.SyncFailed);
        writeBack.Attempts.ShouldBe(5);
        writeBack.NextAttemptAt.ShouldBeNull();
        writeBack.IsDue(now.AddDays(1)).ShouldBeFalse();
    }

    [Fact]
    public void RecordFailure_Rejection_IsSyncFailedAtOnce()
    {
        var writeBack = New();

        var failed = writeBack.RecordFailure("OrgaTex.ConsumptionRejected:Batch.NotFound", retryable: false, _policy, _now);

        failed.ShouldBeTrue();
        writeBack.Status.ShouldBe(WriteBackStatus.SyncFailed);
        writeBack.Attempts.ShouldBe(1);
    }

    [Fact]
    public void RetryManually_FromSyncFailed_IsDueNowWithTheSameKeyAndPayload()
    {
        var writeBack = New();
        var key = writeBack.IdempotencyKey;
        writeBack.RecordFailure("OrgaTex.ConsumptionRejected:Batch.NotFound", retryable: false, _policy, _now);
        var later = _now.AddHours(1);

        var retried = writeBack.RetryManually(later);

        retried.IsError.ShouldBeFalse();
        writeBack.Status.ShouldBe(WriteBackStatus.Pending);
        writeBack.Attempts.ShouldBe(0);
        writeBack.IsDue(later).ShouldBeTrue();
        writeBack.IdempotencyKey.ShouldBe(key);
        writeBack.Lines.ShouldBe(_lines);
    }

    [Fact]
    public void RetryManually_WhilePending_IsRefused()
    {
        var writeBack = New();

        writeBack.RetryManually(_now).FirstError.Code.ShouldBe("WriteBack.NotFailed");
    }

    [Fact]
    public void RetryManually_AfterSent_IsRefusedAndStaysSent()
    {
        var writeBack = New();
        writeBack.RecordSent(_now, _now);

        writeBack.RetryManually(_now).FirstError.Code.ShouldBe("WriteBack.NotFailed");
        writeBack.Status.ShouldBe(WriteBackStatus.Sent);
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 10)]
    [InlineData(3, 20)]
    [InlineData(4, 40)]
    [InlineData(7, 300)] // 320 s is capped at 5 min
    [InlineData(64, 300)] // no overflow however many attempts are configured
    public void RetryPolicy_DoublesFromTheBaseDelayUpToTheCap(int attempt, int seconds) =>
        _policy.DelayAfter(attempt).ShouldBe(TimeSpan.FromSeconds(seconds));
}
