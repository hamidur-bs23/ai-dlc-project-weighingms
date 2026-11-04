namespace WeighingMS.Simulators.Host.OrgaTex;

/// <summary>A batch recipe in weighing sequence (training contract).</summary>
public sealed record RecipeResponse(
    string BatchId,
    string Shade,
    bool TrainingOnly,
    IReadOnlyList<RecipeComponentResponse> Components);
