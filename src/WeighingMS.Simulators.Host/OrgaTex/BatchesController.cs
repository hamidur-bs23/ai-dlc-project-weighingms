using Microsoft.AspNetCore.Mvc;
using WeighingMS.Simulators.Host.Common;
using WeighingMS.Simulators.Host.Faults;
using WeighingMS.Simulators.Host.Seed;

namespace WeighingMS.Simulators.Host.OrgaTex;

/// <summary>
/// Dummy OrgaTEX (workbook STEP 16): batch queue, batch, recipe and consumption receiver.
/// Training contracts only: not claims about the real OrgaTEX interface.
/// </summary>
[ApiController]
[Route("api/batches")]
[Produces("application/json")]
[FaultInjection(FaultTargets.OrgaTex)]
public sealed class BatchesController(SimulatorSeed seed, ConsumptionStore consumption) : ControllerBase
{
    /// <summary>Name of the idempotency header required on consumption posts.</summary>
    public const string IdempotencyHeader = "Idempotency-Key";

    /// <summary>The batch queue released for weighing.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BatchSummaryResponse>>(StatusCodes.Status200OK)]
    public IReadOnlyList<BatchSummaryResponse> GetBatches() => [.. seed.Batches.Select(ToSummary)];

    /// <summary>One batch.</summary>
    [HttpGet("{batchId}")]
    [ProducesResponseType<BatchSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult GetBatch(string batchId)
    {
        var batch = Find(batchId);
        return batch is null ? SimProblems.BatchNotFound(batchId) : Ok(ToSummary(batch));
    }

    /// <summary>The batch recipe in weighing sequence.</summary>
    [HttpGet("{batchId}/recipe")]
    [ProducesResponseType<RecipeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult GetRecipe(string batchId)
    {
        var batch = Find(batchId);
        if (batch is null)
        {
            return SimProblems.BatchNotFound(batchId);
        }

        var components = batch.Components
            .OrderBy(c => c.Sequence)
            .Select(c => new RecipeComponentResponse(
                c.Sequence, c.ChemicalId, c.Code, c.Name, c.DisplayColour, c.TargetQuantity, c.Unit, c.Tolerance, c.SuggestedContainer))
            .ToList();
        return Ok(new RecipeResponse(batch.BatchId, batch.Shade, batch.TrainingOnly, components));
    }

    /// <summary>Receives the actual consumption of a completed batch. Idempotent per <c>Idempotency-Key</c>.</summary>
    [HttpPost("{batchId}/consumption")]
    [ProducesResponseType<ConsumptionReceipt>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public IActionResult PostConsumption(
        string batchId,
        [FromHeader(Name = IdempotencyHeader)] string? idempotencyKey,
        [FromBody] ConsumptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return SimProblems.Create(
                StatusCodes.Status400BadRequest, "Consumption.IdempotencyKeyMissing", "Idempotency-Key header is required");
        }

        if (Find(batchId) is null)
        {
            return SimProblems.BatchNotFound(batchId);
        }

        var (outcome, receipt) = consumption.Store(batchId, idempotencyKey.Trim(), request);
        return outcome == ConsumptionStore.Outcome.KeyConflict
            ? SimProblems.Create(
                StatusCodes.Status409Conflict,
                "Consumption.IdempotencyKeyConflict",
                "Idempotency-Key already used for another batch",
                $"Key was used for batch '{receipt.BatchId}'.")
            : Ok(receipt);
    }

    private SeedBatch? Find(string batchId) =>
        seed.Batches.FirstOrDefault(b => string.Equals(b.BatchId, batchId, StringComparison.OrdinalIgnoreCase));

    private static BatchSummaryResponse ToSummary(SeedBatch b) =>
        new(b.BatchId, b.OrderId, b.Machine, b.Fabric, b.BatchWeightKg, b.Shade, b.Components.Count, b.Status, b.TrainingOnly);
}
