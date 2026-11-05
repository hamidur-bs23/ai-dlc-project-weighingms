namespace WeighingMS.Api.Contracts.Batches;

/// <summary>The recipe snapshot of a batch, in weighing sequence.</summary>
public sealed record RecipeResponse(
    string BatchId,
    string Shade,
    DateTimeOffset LoadedAt,
    bool TrainingOnly,
    IReadOnlyList<RecipeComponentResponse> Components);
