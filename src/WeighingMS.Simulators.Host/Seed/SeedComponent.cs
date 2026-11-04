namespace WeighingMS.Simulators.Host.Seed;

/// <summary>One recipe line of a seed batch (training data).</summary>
public sealed record SeedComponent(
    int Sequence,
    string ChemicalId,
    string Code,
    string Name,
    string DisplayColour,
    decimal TargetQuantity,
    string Unit,
    decimal Tolerance,
    string SuggestedContainer);
