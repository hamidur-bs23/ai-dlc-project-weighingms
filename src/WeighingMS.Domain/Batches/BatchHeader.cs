namespace WeighingMS.Domain.Batches;

/// <summary>The batch fields copied into a recipe snapshot.</summary>
public sealed record BatchHeader(
    string BatchId,
    string OrderId,
    string Machine,
    string Fabric,
    decimal BatchWeightKg,
    string Shade,
    bool TrainingOnly);
