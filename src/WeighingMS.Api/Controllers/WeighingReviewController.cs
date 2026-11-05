using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeighingMS.Api.Common;
using WeighingMS.Api.Contracts.Weighing;
using WeighingMS.Application.Weighing;

namespace WeighingMS.Api.Controllers;

/// <summary>
/// The review list of completed batches (story 014, REVIEW-LIST), newest first, 20 per page; each opens read-only through
/// <c>GET /api/batches/{batchId}/session/summary</c>.
/// </summary>
[Route("api/weighing-sessions")]
[Authorize(Policy = StationPolicies.RequireOperator)]
public sealed class WeighingReviewController(WeighingSessionService weighing) : ApiControllerBase
{
    /// <summary>Completed batches (<c>status=completed</c> is the only status listed in V1).</summary>
    [HttpGet]
    [ProducesResponseType<CompletedBatchPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(status, "completed", StringComparison.Ordinal))
        {
            return Problem([ErrorOr.Error.Validation("Request.Invalid", "status must be 'completed'.", new Dictionary<string, object> { ["field"] = "status" })]);
        }

        var result = await weighing.ListCompletedAsync(page, cancellationToken);
        return Ok(new CompletedBatchPageResponse(
            [.. result.Items.Select(i => new CompletedBatchResponse(i.BatchId, i.OrderId, i.Machine, i.Shade, i.CompletedAt, i.Done, i.Total, i.AllWithinTolerance, i.OverrideCount))],
            result.Page,
            result.PageSize,
            result.Total));
    }
}
