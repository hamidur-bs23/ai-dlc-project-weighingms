using Microsoft.EntityFrameworkCore;
using WeighingMS.Application.WriteBack;
using WeighingMS.Domain.WriteBack;
using WeighingMS.Infrastructure.Persistence;

namespace WeighingMS.Infrastructure.WriteBack;

/// <summary>EF Core implementation of <see cref="IWriteBackRepository"/> (<c>xmin</c> concurrency).</summary>
public sealed class WriteBackRepository(WeighingDbContext db) : IWriteBackRepository
{
    /// <inheritdoc />
    public void Add(ConsumptionWriteBack writeBack) => db.WriteBacks.Add(writeBack);

    /// <inheritdoc />
    public Task<ConsumptionWriteBack?> FindAsync(string batchId, CancellationToken cancellationToken) =>
        db.WriteBacks.FirstOrDefaultAsync(w => w.BatchId == batchId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConsumptionWriteBack>> ListDueAsync(DateTimeOffset now, int take, CancellationToken cancellationToken) =>
        await db.WriteBacks
            .Where(w => w.Status == WriteBackStatus.Pending && (w.NextAttemptAt == null || w.NextAttemptAt <= now))
            .OrderBy(w => w.NextAttemptAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, WriteBackStatus>> StatusesAsync(IReadOnlyCollection<string> batchIds, CancellationToken cancellationToken) =>
        await db.WriteBacks.AsNoTracking()
            .Where(w => batchIds.Contains(w.BatchId))
            .ToDictionaryAsync(w => w.BatchId, w => w.Status, StringComparer.Ordinal, cancellationToken);

    /// <inheritdoc />
    public Task<int> CountUnsyncedAsync(CancellationToken cancellationToken) =>
        db.WriteBacks.CountAsync(w => w.Status != WriteBackStatus.Sent, cancellationToken);

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
            foreach (var entry in db.ChangeTracker.Entries<ConsumptionWriteBack>().ToList())
            {
                entry.State = EntityState.Detached;
            }

            return false;
        }
    }
}
