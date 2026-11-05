using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeighingMS.Api.Common;
using WeighingMS.Api.Contracts.Batches;
using WeighingMS.Api.Contracts.Devices;
using WeighingMS.Application.Batches;
using WeighingMS.Domain.Batches;
using WeighingMS.Domain.Common;

namespace WeighingMS.Api.Controllers;

/// <summary>
/// The batch queue and recipe (workbook STEP 16/19 paths preserved), read from OrgaTEX through
/// <see cref="IOrgatexGateway"/>. The UI holds no batch data of its own (workbook Rule 5).
/// </summary>
[Route("api/batches")]
[Authorize(Policy = StationPolicies.RequireOperator)]
public sealed class BatchesController(BatchService batches) : ApiControllerBase
{
    /// <summary>The batches released for weighing.</summary>
    [HttpGet]
    [ProducesResponseType<BatchListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await batches.GetQueueAsync(cancellationToken);
        return result.Match<IActionResult>(q => Ok(new BatchListResponse([.. q.Select(ToResponse)])), Problem);
    }

    /// <summary>One batch (touch or scanned batch ID).</summary>
    [HttpGet("{batchId}")]
    [ProducesResponseType<BatchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(string batchId, CancellationToken cancellationToken)
    {
        var result = await batches.GetBatchAsync(batchId, cancellationToken);
        return result.Match<IActionResult>(b => Ok(ToResponse(b)), Problem);
    }

    /// <summary>
    /// The recipe snapshot: taken on the first call, then served from WeighingMS even when OrgaTEX is down
    /// (decision RECIPE-SNAPSHOT).
    /// </summary>
    [HttpGet("{batchId}/recipe")]
    [ProducesResponseType<RecipeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Recipe(string batchId, CancellationToken cancellationToken)
    {
        var result = await batches.GetRecipeAsync(batchId, cancellationToken);
        return result.Match<IActionResult>(s => Ok(ToResponse(s)), Problem);
    }

    private static BatchResponse ToResponse(QueuedBatch q) => new(
        q.Batch.BatchId, q.Batch.OrderId, q.Batch.Machine, q.Batch.Fabric, new QuantityResponse(q.Batch.BatchWeightKg, "kg"),
        q.Batch.Shade, q.Batch.ComponentCount, WireNames.Of(q.Status), q.Batch.TrainingOnly);

    private static RecipeResponse ToResponse(RecipeSnapshot s) => new(
        s.BatchId, s.Shade, s.LoadedAt, s.TrainingOnly,
        [.. s.Components.Select(c => new RecipeComponentResponse(
            c.Sequence, c.ChemicalId, c.Code, c.Name, c.DisplayColour, ToWire(c.Target), c.RecipeTolerance is { } t ? ToWire(t) : null, c.SuggestedContainer))]);

    private static QuantityResponse ToWire(Quantity q) => new(q.Value, q.Symbol);
}
