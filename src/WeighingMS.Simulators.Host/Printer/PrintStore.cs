using System.Collections.Concurrent;

namespace WeighingMS.Simulators.Host.Printer;

/// <summary>
/// In-memory record of printed tickets, keyed by <c>Idempotency-Key</c>: a repeated key prints nothing new.
/// </summary>
public sealed class PrintStore(TimeProvider clock)
{
    private readonly ConcurrentDictionary<string, PrintReceipt> _byKey = new(StringComparer.Ordinal);

    /// <summary>Prints the ticket unless its key was seen before; returns the receipt and whether it was new.</summary>
    public (bool Printed, PrintReceipt Receipt) Print(string idempotencyKey, TicketRequest ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        var candidate = new PrintReceipt(
            Guid.NewGuid(), ticket.BatchId, idempotencyKey, ticket.IsReprint, clock.GetUtcNow(), ticket.Lines, ticket.Barcode);
        var stored = _byKey.GetOrAdd(idempotencyKey, candidate);
        return (ReferenceEquals(stored, candidate), stored);
    }

    /// <summary>All printed tickets in print order.</summary>
    public IReadOnlyList<PrintReceipt> All() => [.. _byKey.Values.OrderBy(r => r.PrintedAt)];

    /// <summary>Forgets every printed ticket.</summary>
    public void Reset() => _byKey.Clear();
}
