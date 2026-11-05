using ErrorOr;

namespace WeighingMS.Domain.Batches;

/// <summary>Batch and recipe errors (codes in <c>Area.Reason</c> form).</summary>
public static class BatchErrors
{
    /// <summary>The batch is not in the OrgaTEX queue (404).</summary>
    public static Error NotFound(string batchId) =>
        Error.NotFound("Batch.NotFound", $"Batch '{batchId}' was not found.", new Dictionary<string, object> { ["batchId"] = batchId });

    /// <summary>OrgaTEX sent a recipe WeighingMS cannot use (502: the fault is upstream, decision RECIPE-SNAPSHOT).</summary>
    public static Error InvalidRecipe(string batchId, string reason) =>
        Error.Custom(502, "OrgaTex.InvalidRecipe", $"The recipe of batch '{batchId}' is invalid: {reason}.", new Dictionary<string, object> { ["batchId"] = batchId });
}
