using WeighingMS.Domain.Tickets;

namespace WeighingMS.Application.Tickets;

/// <summary>Stores tickets and their print jobs.</summary>
public interface ITicketRepository
{
    /// <summary>Adds a ticket to the current unit of work (saved with the completion).</summary>
    void Add(Ticket ticket);

    /// <summary>Adds a print attempt to the current unit of work.</summary>
    void AddJob(PrintJob job);

    /// <summary>The batch's ticket, if it has one.</summary>
    Task<Ticket?> FindAsync(string batchId, CancellationToken cancellationToken);

    /// <summary>The ticket status of each of these batches that has a ticket.</summary>
    Task<IReadOnlyDictionary<string, TicketStatus>> StatusesAsync(IReadOnlyCollection<string> batchIds, CancellationToken cancellationToken);

    /// <summary>Saves; false when another press changed the ticket first (optimistic concurrency).</summary>
    Task<bool> SaveAsync(CancellationToken cancellationToken);
}
