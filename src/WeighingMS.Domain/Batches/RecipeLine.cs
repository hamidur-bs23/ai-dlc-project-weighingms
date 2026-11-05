namespace WeighingMS.Domain.Batches;

/// <summary>One recipe line as OrgaTEX sent it, before validation.</summary>
public sealed record RecipeLine(
    int Sequence,
    string ChemicalId,
    string Code,
    string Name,
    string DisplayColour,
    decimal Target,
    string Unit,
    decimal? Tolerance,
    string? SuggestedContainer);
