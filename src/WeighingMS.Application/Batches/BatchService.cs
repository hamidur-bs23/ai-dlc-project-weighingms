using ErrorOr;
using WeighingMS.Application.Abstractions;
using WeighingMS.Application.WriteBack;
using WeighingMS.Domain.Batches;
using WeighingMS.Domain.WriteBack;

namespace WeighingMS.Application.Batches;

/// <summary>
/// Batch use cases: the queue (live from OrgaTEX, with WeighingMS status), one batch, and the recipe snapshot
/// (taken on first request, served from the database afterwards even if OrgaTEX is down).
/// </summary>
public sealed class BatchService(IOrgatexGateway orgatex, IRecipeSnapshotStore snapshots, IPlantClock clock, IWriteBackRepository writeBacks)
{
    /// <summary>The released batches with their status. No caching: always read from OrgaTEX.</summary>
    public async Task<ErrorOr<IReadOnlyList<QueuedBatch>>> GetQueueAsync(CancellationToken cancellationToken)
    {
        var batches = await orgatex.GetBatchesAsync(cancellationToken);
        if (batches.IsError)
        {
            return batches.Errors;
        }

        var ids = batches.Value.Select(b => b.BatchId).ToList();
        var opened = await snapshots.WithSnapshotAsync(ids, cancellationToken);
        var synced = await writeBacks.StatusesAsync(ids, cancellationToken);
        return batches.Value.Select(b => new QueuedBatch(b, StatusOf(b.BatchId, opened, synced))).ToList();
    }

    /// <summary>One batch with its status.</summary>
    public async Task<ErrorOr<QueuedBatch>> GetBatchAsync(string batchId, CancellationToken cancellationToken)
    {
        var batch = await orgatex.GetBatchAsync(batchId, cancellationToken);
        if (batch.IsError)
        {
            return batch.Errors;
        }

        var opened = await snapshots.WithSnapshotAsync([batch.Value.BatchId], cancellationToken);
        var synced = await writeBacks.StatusesAsync([batch.Value.BatchId], cancellationToken);
        return new QueuedBatch(batch.Value, StatusOf(batch.Value.BatchId, opened, synced));
    }

    /// <summary>
    /// The recipe snapshot: returned from the database if it exists; otherwise fetched from OrgaTEX, validated,
    /// stored and returned (decision RECIPE-SNAPSHOT). Invalid OrgaTEX data stores nothing.
    /// </summary>
    public async Task<ErrorOr<RecipeSnapshot>> GetRecipeAsync(string batchId, CancellationToken cancellationToken)
    {
        if (await snapshots.FindAsync(batchId, cancellationToken) is { } existing)
        {
            return existing;
        }

        var batch = await orgatex.GetBatchAsync(batchId, cancellationToken);
        if (batch.IsError)
        {
            return batch.Errors;
        }

        var recipe = await orgatex.GetRecipeAsync(batchId, cancellationToken);
        if (recipe.IsError)
        {
            return recipe.Errors;
        }

        var b = batch.Value;
        var header = new BatchHeader(b.BatchId, b.OrderId, b.Machine, b.Fabric, b.BatchWeightKg, b.Shade, b.TrainingOnly || recipe.Value.TrainingOnly);
        var snapshot = RecipeSnapshot.Create(header, recipe.Value.Lines, clock.UtcNow);
        if (snapshot.IsError)
        {
            return snapshot.Errors;
        }

        return await snapshots.AddOrGetExistingAsync(snapshot.Value, cancellationToken);
    }

    /// <summary>
    /// The WeighingMS status of a batch: a write-back exists once it completed (Complete / SyncPending / SyncFailed);
    /// otherwise Weighing when opened, else Queued.
    /// </summary>
    private static BatchStatus StatusOf(string batchId, IReadOnlySet<string> opened, IReadOnlyDictionary<string, WriteBackStatus> synced)
    {
        if (!synced.TryGetValue(batchId, out var status))
        {
            return opened.Contains(batchId) ? BatchStatus.Weighing : BatchStatus.Queued;
        }

        return status switch
        {
            WriteBackStatus.Sent => BatchStatus.Complete,
            WriteBackStatus.SyncFailed => BatchStatus.SyncFailed,
            _ => BatchStatus.SyncPending,
        };
    }
}
