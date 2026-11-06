using WeighingMS.Domain.WriteBack;

namespace WeighingMS.Application.WriteBack;

/// <summary>Persisted OrgaTEX write-backs (the outbox, one per batch).</summary>
public interface IWriteBackRepository
{
    /// <summary>Adds a write-back to the current unit of work (saved with the batch's completion).</summary>
    void Add(ConsumptionWriteBack writeBack);

    /// <summary>The write-back of a batch (tracked), or null.</summary>
    Task<ConsumptionWriteBack?> FindAsync(string batchId, CancellationToken cancellationToken);

    /// <summary>Pending write-backs due by <paramref name="now"/>, oldest due first (tracked).</summary>
    Task<IReadOnlyList<ConsumptionWriteBack>> ListDueAsync(DateTimeOffset now, int take, CancellationToken cancellationToken);

    /// <summary>The status of each given batch's write-back, if it has one.</summary>
    Task<IReadOnlyDictionary<string, WriteBackStatus>> StatusesAsync(IReadOnlyCollection<string> batchIds, CancellationToken cancellationToken);

    /// <summary>How many write-backs are not yet sent (pending or sync failed).</summary>
    Task<int> CountUnsyncedAsync(CancellationToken cancellationToken);

    /// <summary>Saves the changes; false when another change to the same write-back was saved first.</summary>
    Task<bool> SaveAsync(CancellationToken cancellationToken);
}

/// <summary>Wakes the background dispatcher (for example right after a batch completes).</summary>
public interface IWriteBackSignal
{
    /// <summary>Asks the dispatcher to look for due write-backs now.</summary>
    void Notify();
}
