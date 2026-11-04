using System.Collections.Concurrent;

namespace WeighingMS.Simulators.Host.OrgaTex;

/// <summary>
/// In-memory store of received consumption, keyed by <c>Idempotency-Key</c>.
/// A repeated key returns the first receipt and stores nothing new.
/// </summary>
public sealed class ConsumptionStore(TimeProvider clock)
{
    private readonly ConcurrentDictionary<string, ConsumptionReceipt> _byKey = new(StringComparer.Ordinal);

    /// <summary>Outcome of a store attempt.</summary>
    public enum Outcome
    {
        /// <summary>A new post was stored.</summary>
        Stored,

        /// <summary>The key was already used for the same batch; the original receipt is returned.</summary>
        Replayed,

        /// <summary>The key was already used for a different batch.</summary>
        KeyConflict,
    }

    /// <summary>Stores a post unless its key was seen before.</summary>
    public (Outcome Outcome, ConsumptionReceipt Receipt) Store(string batchId, string idempotencyKey, ConsumptionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var candidate = new ConsumptionReceipt(batchId, idempotencyKey, clock.GetUtcNow(), request.Lines);
        var stored = _byKey.GetOrAdd(idempotencyKey, candidate);
        if (ReferenceEquals(stored, candidate))
        {
            return (Outcome.Stored, stored);
        }

        return stored.BatchId == batchId ? (Outcome.Replayed, stored) : (Outcome.KeyConflict, stored);
    }

    /// <summary>All receipts in the order received.</summary>
    public IReadOnlyList<ConsumptionReceipt> All() => [.. _byKey.Values.OrderBy(r => r.ReceivedAt)];
}
