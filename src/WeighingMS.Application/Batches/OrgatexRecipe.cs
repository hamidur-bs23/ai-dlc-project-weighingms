using WeighingMS.Domain.Batches;

namespace WeighingMS.Application.Batches;

/// <summary>A recipe as OrgaTEX sends it (not yet validated).</summary>
public sealed record OrgatexRecipe(string BatchId, string Shade, bool TrainingOnly, IReadOnlyList<RecipeLine> Lines);
