using ErrorOr;

namespace WeighingMS.Application.Batches;

/// <summary>
/// The single port to OrgaTEX (decision at CHECKPOINT 3; boundary B1). This bolt uses the read operations; the
/// consumption write-back operation is added by bolt 011. Adapters translate the training contract and every
/// transport failure into <see cref="OrgatexErrors"/>.
/// </summary>
public interface IOrgatexGateway
{
    /// <summary>The released batch queue.</summary>
    Task<ErrorOr<IReadOnlyList<OrgatexBatch>>> GetBatchesAsync(CancellationToken cancellationToken);

    /// <summary>One batch (<c>Batch.NotFound</c> if absent).</summary>
    Task<ErrorOr<OrgatexBatch>> GetBatchAsync(string batchId, CancellationToken cancellationToken);

    /// <summary>The batch recipe (<c>Batch.NotFound</c> if absent).</summary>
    Task<ErrorOr<OrgatexRecipe>> GetRecipeAsync(string batchId, CancellationToken cancellationToken);
}
