using Microsoft.EntityFrameworkCore;
using WeighingMS.Application.Tickets;
using WeighingMS.Domain.Tickets;
using WeighingMS.Infrastructure.Persistence;

namespace WeighingMS.Infrastructure.Tickets;

/// <summary>EF Core implementation of <see cref="ITicketRepository"/> (<c>xmin</c> concurrency on the ticket).</summary>
public sealed class TicketRepository(WeighingDbContext db) : ITicketRepository
{
    /// <inheritdoc />
    public void Add(Ticket ticket) => db.Tickets.Add(ticket);

    /// <inheritdoc />
    public void AddJob(PrintJob job) => db.PrintJobs.Add(job);

    /// <inheritdoc />
    public Task<Ticket?> FindAsync(string batchId, CancellationToken cancellationToken) =>
        db.Tickets.FirstOrDefaultAsync(t => t.BatchId == batchId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, TicketStatus>> StatusesAsync(IReadOnlyCollection<string> batchIds, CancellationToken cancellationToken) =>
        await db.Tickets.AsNoTracking()
            .Where(t => batchIds.Contains(t.BatchId))
            .ToDictionaryAsync(t => t.BatchId, t => t.Status, StringComparer.Ordinal, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return false;
        }
    }
}
