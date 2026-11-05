using ErrorOr;
using WeighingMS.Domain.Weighing;

namespace WeighingMS.Application.Weighing;

/// <summary>Persisted weighing sessions (one per batch).</summary>
public interface IWeighingSessionRepository
{
    /// <summary>The session of a batch with its components (tracked for changes), or null.</summary>
    Task<WeighingSession?> FindAsync(string batchId, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a new session with the pending changes (audit entries). If another request started a session for the
    /// same batch first, nothing is stored and that session is returned instead (<c>Started = false</c>).
    /// </summary>
    Task<(WeighingSession Session, bool Started)> AddOrGetExistingAsync(WeighingSession session, CancellationToken cancellationToken);

    /// <summary>Saves changes to a loaded session; a concurrent change saved first → <see cref="WeighingErrors.ConcurrencyConflict"/>.</summary>
    Task<ErrorOr<Success>> SaveAsync(CancellationToken cancellationToken);
}
