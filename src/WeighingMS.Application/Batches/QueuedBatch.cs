namespace WeighingMS.Application.Batches;

/// <summary>A queue row: the OrgaTEX batch and its WeighingMS status.</summary>
public sealed record QueuedBatch(OrgatexBatch Batch, BatchStatus Status);
