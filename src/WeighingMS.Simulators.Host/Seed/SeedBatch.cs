namespace WeighingMS.Simulators.Host.Seed;

/// <summary>A training batch with its recipe, as released by the dummy OrgaTEX.</summary>
public sealed record SeedBatch(
    string BatchId,
    string OrderId,
    string Machine,
    string Fabric,
    decimal BatchWeightKg,
    string Shade,
    string Status,
    bool TrainingOnly,
    IReadOnlyList<SeedComponent> Components);
