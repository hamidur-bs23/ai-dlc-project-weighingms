using ErrorOr;

namespace WeighingMS.Domain.WriteBack;

/// <summary>
/// The store-and-forward write-back of one completed batch's consumption (PD-01, FR-20). Created in the completing
/// transaction; posted later with an <see cref="IdempotencyKey"/> that never changes, so OrgaTEX stores it once however
/// often it is sent. Pending → Sent, or → SyncFailed after the retry limit or a rejection; SyncFailed → Pending by a
/// manual retry.
/// </summary>
public sealed class ConsumptionWriteBack
{
    private List<ConsumptionLine> _lines = [];

    private ConsumptionWriteBack()
    {
        BatchId = string.Empty;
    }

    /// <summary>Identifier (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>The batch (one write-back per batch).</summary>
    public string BatchId { get; private set; }

    /// <summary>The weighing session that completed.</summary>
    public Guid SessionId { get; private set; }

    /// <summary>Sent as the <c>Idempotency-Key</c> header on every attempt; never regenerated.</summary>
    public Guid IdempotencyKey { get; private set; }

    /// <summary>The consumption per chemical.</summary>
    public IReadOnlyList<ConsumptionLine> Lines => _lines;

    /// <summary>Status.</summary>
    public WriteBackStatus Status { get; private set; }

    /// <summary>Attempts made since creation or the last manual retry.</summary>
    public int Attempts { get; private set; }

    /// <summary>When the next attempt is due (pending only).</summary>
    public DateTimeOffset? NextAttemptAt { get; private set; }

    /// <summary>The last failure's error code, if any.</summary>
    public string? LastErrorCode { get; private set; }

    /// <summary>When the write-back was created (the batch's completion time).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When OrgaTEX confirmed it.</summary>
    public DateTimeOffset? SentAt { get; private set; }

    /// <summary>When OrgaTEX says it received it (from its receipt).</summary>
    public DateTimeOffset? ReceivedAt { get; private set; }

    /// <summary>Creates a pending write-back, due immediately.</summary>
    public static ConsumptionWriteBack Create(string batchId, Guid sessionId, IReadOnlyCollection<ConsumptionLine> lines, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchId);
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count == 0)
        {
            throw new ArgumentException("A write-back needs at least one line.", nameof(lines));
        }

        var writeBack = new ConsumptionWriteBack
        {
            Id = Guid.CreateVersion7(),
            BatchId = batchId,
            SessionId = sessionId,
            IdempotencyKey = Guid.CreateVersion7(),
            Status = WriteBackStatus.Pending,
            CreatedAt = now.ToUniversalTime(),
            NextAttemptAt = now.ToUniversalTime(),
            _lines = [.. lines],
        };
        return writeBack;
    }

    /// <summary>Whether it should be attempted now.</summary>
    public bool IsDue(DateTimeOffset now) => Status == WriteBackStatus.Pending && (NextAttemptAt is null || NextAttemptAt <= now);

    /// <summary>OrgaTEX confirmed the post.</summary>
    public void RecordSent(DateTimeOffset receivedAt, DateTimeOffset now)
    {
        Attempts++;
        Status = WriteBackStatus.Sent;
        SentAt = now.ToUniversalTime();
        ReceivedAt = receivedAt.ToUniversalTime();
        NextAttemptAt = null;
        LastErrorCode = null;
    }

    /// <summary>
    /// An attempt failed. A transient failure is retried after the policy's back-off until the limit; a rejection
    /// (not retryable, WRITEBACK-TRANSIENT) or the limit makes it SyncFailed. Returns true when it just became SyncFailed.
    /// </summary>
    public bool RecordFailure(string errorCode, bool retryable, RetryPolicy policy, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(policy);
        Attempts++;
        LastErrorCode = errorCode;
        if (!retryable || Attempts >= policy.MaxAttempts)
        {
            Status = WriteBackStatus.SyncFailed;
            NextAttemptAt = null;
            return true;
        }

        NextAttemptAt = now.ToUniversalTime() + policy.DelayAfter(Attempts);
        return false;
    }

    /// <summary>Manual retry (WRITEBACK-RETRY-ANYONE): only from SyncFailed; same payload and key; due now.</summary>
    public ErrorOr<Success> RetryManually(DateTimeOffset now)
    {
        if (Status != WriteBackStatus.SyncFailed)
        {
            return Error.Conflict("WriteBack.NotFailed", $"The write-back of '{BatchId}' is {Status}; only a failed one can be retried.");
        }

        Status = WriteBackStatus.Pending;
        Attempts = 0;
        NextAttemptAt = now.ToUniversalTime();
        return Result.Success;
    }
}
