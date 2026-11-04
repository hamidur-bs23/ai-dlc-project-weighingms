namespace WeighingMS.Simulators.Host.Seed;

/// <summary>The complete training seed loaded from <c>Seed/seed.json</c>.</summary>
public sealed record SimulatorSeed(IReadOnlyList<SeedBatch> Batches, IReadOnlyList<SeedScale> Scales);
