using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeighingMS.Api.Common;
using WeighingMS.Api.Contracts.Weighing;
using WeighingMS.Application.WriteBack;

namespace WeighingMS.Api.Controllers;

/// <summary>
/// The OrgaTEX write-back of a completed batch (bolt 011): its state, and the manual Retry of a sync-failed one
/// (WRITEBACK-RETRY-ANYONE: same payload, same Idempotency-Key, so it can never post twice).
/// </summary>
[Route("api/batches/{batchId}/session/write-back")]
[Authorize(Policy = StationPolicies.RequireOperator)]
public sealed class WriteBackController(WriteBackService writeBacks) : ApiControllerBase
{
    /// <summary>The batch's write-back state (<c>notAvailable</c> until it completes).</summary>
    [HttpGet]
    [ProducesResponseType<OrgatexStateResponse>(StatusCodes.Status200OK)]
    public async Task<OrgatexStateResponse> Get(string batchId, CancellationToken cancellationToken) =>
        ToResponse(await writeBacks.GetAsync(batchId, cancellationToken));

    /// <summary>Retries a sync-failed write-back; 409 <c>WriteBack.NotFailed</c> otherwise.</summary>
    [HttpPost("retry")]
    [ProducesResponseType<OrgatexStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Retry(string batchId, CancellationToken cancellationToken)
    {
        var result = await writeBacks.RetryAsync(batchId, StationClaims.BadgeOf(User) ?? string.Empty, cancellationToken);
        return result.Match<IActionResult>(v => Ok(ToResponse(v)), Problem);
    }

    /// <summary>The wire form of a write-back view (shared with the batch summary).</summary>
    internal static OrgatexStateResponse ToResponse(WriteBackView? view) =>
        view is null
            ? OrgatexStateResponse.NotAvailable
            : new OrgatexStateResponse(WireNames.Of(view.Status), view.SentAt, view.Attempts, view.NextAttemptAt, view.LastErrorCode);
}
