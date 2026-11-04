namespace WeighingMS.Simulators.Host.OrgaTex;

/// <summary>One recipe line (training contract). Quantities and tolerance are in <see cref="Unit"/>.</summary>
public sealed record RecipeComponentResponse(
    int Sequence,
    string ChemicalId,
    string Code,
    string Name,
    string DisplayColour,
    decimal TargetQuantity,
    string Unit,
    decimal Tolerance,
    string SuggestedContainer);
