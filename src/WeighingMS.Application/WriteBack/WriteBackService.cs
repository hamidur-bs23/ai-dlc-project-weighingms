using ErrorOr;
using WeighingMS.Application.Abstractions;
using WeighingMS.Domain.Audit;
using WeighingMS.Domain.WriteBack;

namespace WeighingMS.Application.WriteBack;

/// <summary>What the screen shows about a batch's write-back (story 004).</summary>
public sealed record WriteBackView(WriteBackStatus Status, int Attempts, DateTimeOffset? NextAttemptAt, DateTimeOffset? SentAt, string? LastErrorCode);

/// <summary>Write-back status, the unsynced count and manual retry (stories 003–004).</summary>
public sealed class WriteBackService(IWriteBackRepository writeBacks, IWriteBackSignal signal, IAuditTrail audit, IPlantClock clock)
{
    /// <summary>The batch's write-back, or null when it has none (the batch isn't complete).</summary>
    public async Task<WriteBackView?> GetAsync(string batchId, CancellationToken cancellationToken) =>
        await writeBacks.FindAsync(batchId, cancellationToken) is { } w ? View(w) : null;

    /// <summary>Write-backs not yet sent (pending or sync failed), for the status strip.</summary>
    public Task<int> CountUnsyncedAsync(CancellationToken cancellationToken) => writeBacks.CountUnsyncedAsync(cancellationToken);

    /// <summary>Manual retry of a sync-failed write-back (WRITEBACK-RETRY-ANYONE): same payload, same key; audited.</summary>
    public async Task<ErrorOr<WriteBackView>> RetryAsync(string batchId, string operatorBadgeId, CancellationToken cancellationToken)
    {
        if (await writeBacks.FindAsync(batchId, cancellationToken) is not { } writeBack)
        {
            return Error.NotFound("WriteBack.NotFound", $"Batch '{batchId}' has no OrgaTEX write-back.");
        }

        var retried = writeBack.RetryManually(clock.UtcNow);
        if (retried.IsError)
        {
            return retried.Errors;
        }

        audit.Append(AuditEntry.Record(AuditEventType.WriteBackRetried, operatorBadgeId, clock.UtcNow, batchId));
        if (!await writeBacks.SaveAsync(cancellationToken))
        {
            return Error.Conflict("Concurrency.Conflict", "The write-back changed meanwhile. Reload and try again.");
        }

        signal.Notify();
        return View(writeBack);
    }

    /// <summary>The view of a write-back.</summary>
    public static WriteBackView View(ConsumptionWriteBack w)
    {
        ArgumentNullException.ThrowIfNull(w);
        return new WriteBackView(w.Status, w.Attempts, w.NextAttemptAt, w.SentAt, w.LastErrorCode);
    }
}
