namespace WeighingMS.Application.Batches;

/// <summary>A batch as OrgaTEX releases it.</summary>
public sealed record OrgatexBatch(
    string BatchId,
    string OrderId,
    string Machine,
    string Fabric,
    decimal BatchWeightKg,
    string Shade,
    int ComponentCount,
    bool TrainingOnly);
