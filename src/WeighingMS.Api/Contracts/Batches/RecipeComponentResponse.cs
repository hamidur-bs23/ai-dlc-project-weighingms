using WeighingMS.Api.Contracts.Devices;

namespace WeighingMS.Api.Contracts.Batches;

/// <summary>One recipe line; <c>tolerance</c> is null when the recipe gives none (the configured fallback applies).</summary>
public sealed record RecipeComponentResponse(
    int Sequence,
    string ChemicalId,
    string Code,
    string Name,
    string DisplayColour,
    QuantityResponse Target,
    QuantityResponse? Tolerance,
    string? SuggestedContainer);
