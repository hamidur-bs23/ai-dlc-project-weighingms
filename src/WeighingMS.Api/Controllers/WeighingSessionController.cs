using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeighingMS.Api.Common;
using WeighingMS.Api.Contracts.Devices;
using WeighingMS.Api.Contracts.Weighing;
using WeighingMS.Application.Weighing;
using WeighingMS.Domain.Common;
using WeighingMS.Domain.Weighing;

namespace WeighingMS.Api.Controllers;

/// <summary>
/// The weighing session of a batch (bolt 007): start or resume, read, select a component, and the classified live
/// reading of the active component (decisions SESSION-START, WEIGH-READING). Every rule is decided on the server.
/// </summary>
[Route("api/batches/{batchId}/session")]
[Authorize(Policy = StationPolicies.RequireOperator)]
public sealed class WeighingSessionController(WeighingSessionService weighing) : ApiControllerBase
{
    /// <summary>Starts the batch's weighing session (201), or resumes the existing one (200).</summary>
    [HttpPost]
    [ProducesResponseType<WeighingSessionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<WeighingSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> StartOrResume(string batchId, CancellationToken cancellationToken)
    {
        var result = await weighing.StartOrResumeAsync(batchId, OperatorBadge, cancellationToken);
        if (result.IsError)
        {
            return Problem(result.Errors);
        }

        var (session, started) = result.Value;
        return started
            ? CreatedAtAction(nameof(Get), new { batchId = session.BatchId }, ToResponse(session))
            : Ok(ToResponse(session));
    }

    /// <summary>The batch's weighing session.</summary>
    [HttpGet]
    [ProducesResponseType<WeighingSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string batchId, CancellationToken cancellationToken)
    {
        var result = await weighing.GetAsync(batchId, cancellationToken);
        return result.Match<IActionResult>(s => Ok(ToResponse(s)), Problem);
    }

    /// <summary>Makes a pending component the active one (FR-4); switching abandons the current attempt.</summary>
    [HttpPost("selection")]
    [ProducesResponseType<WeighingSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Select(string batchId, [FromBody] SelectComponentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Sequence is not { } sequence)
        {
            return Problem([Error.Validation("Request.Invalid", "sequence is required.", new Dictionary<string, object> { ["field"] = "sequence" })]);
        }

        var result = await weighing.SelectAsync(batchId, sequence, OperatorBadge, cancellationToken);
        return result.Match<IActionResult>(s => Ok(ToResponse(s)), Problem);
    }

    /// <summary>The active component's live reading from its own scale, classified (polled by the Weigh screen).</summary>
    [HttpGet("reading")]
    [ProducesResponseType<WeighingReadingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Reading(string batchId, CancellationToken cancellationToken)
    {
        var result = await weighing.ReadCurrentAsync(batchId, cancellationToken);
        return result.Match<IActionResult>(r => Ok(ToResponse(r)), Problem);
    }

    private string OperatorBadge => StationClaims.BadgeOf(User) ?? string.Empty;

    private static WeighingSessionResponse ToResponse(WeighingSession s) => new(
        s.BatchId,
        WireNames.Of(s.Status),
        s.StartedAt,
        s.StartedBy,
        new ProgressResponse(s.DoneCount, s.Components.Count),
        s.Suggested?.Sequence,
        s.ActiveSequence,
        [.. s.Components.Select(ToResponse)]);

    private static WeighingComponentResponse ToResponse(ComponentWeighing c)
    {
        var band = c.Band;
        var tolerance = band is null
            ? new ComponentToleranceResponse("blocked", null, null, null, null)
            : new ComponentToleranceResponse("resolved", SourceOf(c), ToWire(band.PlusMinus), ToWire(band.Lower), ToWire(band.Upper));
        var scale = c.Scale;
        return new WeighingComponentResponse(
            c.Sequence,
            c.ChemicalId,
            c.Code,
            c.Name,
            ToWire(c.Target),
            WireNames.Of(c.State),
            new ComponentScaleResponse(c.ScaleId, WireNames.Of(c.ScaleClass), Quantity.SymbolOf(scale.Unit), ToWire(scale.Resolution), Automatic: true),
            tolerance,
            c.SuggestedContainer);
    }

    private static WeighingReadingResponse ToResponse(CurrentReading r)
    {
        var classification = r.Classification;
        var band = classification?.Band ?? r.Component.Band;
        var net = r.Reading.Net is { } raw ? new QuantityResponse(raw.Value, raw.Unit) : null;
        if (classification is not null)
        {
            net = ToWire(classification.Net); // rounded to the scale resolution, as classified
        }

        return new WeighingReadingResponse(
            r.Component.Sequence,
            r.Component.ScaleId,
            net,
            r.Reading.Stable,
            WireNames.Of(r.Reading.Validity),
            r.Reading.ReadAt,
            classification is not null ? WireNames.Of(classification.Class) : null,
            classification is not null ? ToWire(classification.Remaining) : null,
            band is not null ? new ToleranceBandResponse(ToWire(band.Target), ToWire(band.PlusMinus), ToWire(band.Lower), ToWire(band.Upper)) : null,
            r.Component.Tolerance.IsBlocked ? "blocked" : "resolved");
    }

    private static string? SourceOf(ComponentWeighing c) => c.ToleranceSource is { } source ? WireNames.Of(source) : null;

    private static QuantityResponse ToWire(Quantity q) => new(q.Value, q.Symbol);
}
