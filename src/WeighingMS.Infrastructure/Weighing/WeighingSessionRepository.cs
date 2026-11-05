using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WeighingMS.Application.Weighing;
using WeighingMS.Domain.Weighing;
using WeighingMS.Infrastructure.Persistence;

namespace WeighingMS.Infrastructure.Weighing;

/// <summary>EF Core implementation of <see cref="IWeighingSessionRepository"/> (xmin optimistic concurrency).</summary>
public sealed class WeighingSessionRepository(WeighingDbContext db) : IWeighingSessionRepository
{
    /// <inheritdoc />
    public Task<WeighingSession?> FindAsync(string batchId, CancellationToken cancellationToken) =>
        db.WeighingSessions.Include("_components").FirstOrDefaultAsync(s => s.BatchId == batchId, cancellationToken);

    /// <inheritdoc />
    public async Task<(WeighingSession Session, bool Started)> AddOrGetExistingAsync(WeighingSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        db.WeighingSessions.Add(session);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return (session, true);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Another request started the same batch first: drop this session and its audit entries; the first wins.
            foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList())
            {
                entry.State = EntityState.Detached;
            }

            var existing = await FindAsync(session.BatchId, cancellationToken)
                ?? throw new InvalidOperationException($"Session for '{session.BatchId}' vanished after a unique conflict.", ex);
            return (existing, false);
        }
    }

    /// <inheritdoc />
    public async Task<ErrorOr<Success>> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            foreach (var entry in db.ChangeTracker.Entries().ToList())
            {
                entry.State = EntityState.Detached;
            }

            return WeighingErrors.ConcurrencyConflict;
        }
    }
}
