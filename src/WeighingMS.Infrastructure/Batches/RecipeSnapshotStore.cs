using Microsoft.EntityFrameworkCore;
using Npgsql;
using WeighingMS.Application.Batches;
using WeighingMS.Domain.Batches;
using WeighingMS.Infrastructure.Persistence;

namespace WeighingMS.Infrastructure.Batches;

/// <summary>EF Core implementation of <see cref="IRecipeSnapshotStore"/>.</summary>
public sealed class RecipeSnapshotStore(WeighingDbContext db) : IRecipeSnapshotStore
{
    /// <inheritdoc />
    public Task<RecipeSnapshot?> FindAsync(string batchId, CancellationToken cancellationToken) =>
        db.RecipeSnapshots.AsNoTracking().Include("_components").FirstOrDefaultAsync(s => s.BatchId == batchId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlySet<string>> WithSnapshotAsync(IReadOnlyCollection<string> batchIds, CancellationToken cancellationToken)
    {
        var found = await db.RecipeSnapshots.AsNoTracking()
            .Where(s => batchIds.Contains(s.BatchId))
            .Select(s => s.BatchId)
            .ToListAsync(cancellationToken);
        return found.ToHashSet(StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public async Task<RecipeSnapshot> AddOrGetExistingAsync(RecipeSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        db.RecipeSnapshots.Add(snapshot);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return snapshot;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Another request snapshotted the same batch first: the first snapshot wins.
            db.Entry(snapshot).State = EntityState.Detached;
            return await FindAsync(snapshot.BatchId, cancellationToken)
                ?? throw new InvalidOperationException($"Snapshot for '{snapshot.BatchId}' vanished after a unique conflict.", ex);
        }
    }
}
