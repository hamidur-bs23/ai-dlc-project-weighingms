using WeighingMS.Domain.Batches;

namespace WeighingMS.Application.Batches;

/// <summary>Persisted recipe snapshots (one per batch).</summary>
public interface IRecipeSnapshotStore
{
    /// <summary>The snapshot of a batch, if one was taken.</summary>
    Task<RecipeSnapshot?> FindAsync(string batchId, CancellationToken cancellationToken);

    /// <summary>Which of the given batches already have a snapshot.</summary>
    Task<IReadOnlySet<string>> WithSnapshotAsync(IReadOnlyCollection<string> batchIds, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a new snapshot; if another request stored one for the same batch first, returns that one instead
    /// (the first snapshot wins and never changes).
    /// </summary>
    Task<RecipeSnapshot> AddOrGetExistingAsync(RecipeSnapshot snapshot, CancellationToken cancellationToken);
}
