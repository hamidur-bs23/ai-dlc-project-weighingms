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
            return MissingSequence();
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

    /// <summary>Captures the lot (scanned or typed) for the active component (story 004).</summary>
    [HttpPost("attempt/lot")]
    [ProducesResponseType<WeighingSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CaptureLot(string batchId, [FromBody] CaptureLotRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Sequence is not { } sequence)
        {
            return MissingSequence();
        }

        var result = await weighing.CaptureLotAsync(batchId, sequence, request.Lot, OperatorBadge, cancellationToken);
        return result.Match<IActionResult>(s => Ok(ToResponse(s)), Problem);
    }

    /// <summary>Captures the container for the active component (story 004).</summary>
    [HttpPost("attempt/container")]
    [ProducesResponseType<WeighingSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CaptureContainer(string batchId, [FromBody] CaptureContainerRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Sequence is not { } sequence)
        {
            return MissingSequence();
        }

        var result = await weighing.CaptureContainerAsync(batchId, sequence, request.Container, OperatorBadge, cancellationToken);
        return result.Match<IActionResult>(s => Ok(ToResponse(s)), Problem);
    }

    /// <summary>Tares the active component's own scale and stores the tare (SESSION-TARE); again = re-tare (F2).</summary>
    [HttpPost("tare")]
    [ProducesResponseType<WeighingSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Tare(string batchId, [FromBody] ComponentCommandRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Sequence is not { } sequence)
        {
            return MissingSequence();
        }

        var result = await weighing.TareAsync(batchId, sequence, OperatorBadge, cancellationToken);
        return result.Match<IActionResult>(s => Ok(ToResponse(s)), Problem);
    }

    /// <summary>
    /// Confirms the named component (F1, story 008). The server reads the scale itself and re-checks the rule; on success
    /// one immutable record is created (201) and the next pending component becomes active.
    /// </summary>
    [HttpPost("confirmation")]
    [ProducesResponseType<ConfirmationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Confirm(string batchId, [FromBody] ComponentCommandRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Sequence is not { } sequence)
        {
            return MissingSequence();
        }

        var result = await weighing.ConfirmAsync(batchId, sequence, OperatorBadge, cancellationToken);
        return result.Match<IActionResult>(
            r => CreatedAtAction(nameof(Get), new { batchId = r.Session.BatchId }, new ConfirmationResponse(ToResponse(r.Record), ToResponse(r.Session))),
            Problem);
    }

    /// <summary>
    /// Supervisor override of an Underweight or Overweight reading (story 011, PD-05 option 1): needs the single-use grant
    /// from <c>POST /api/session/step-up</c> (another supervisor, four eyes) and a reason. Creates one record with the true
    /// class, the surplus and OVR (201).
    /// </summary>
    [HttpPost("override")]
    [ProducesResponseType<ConfirmationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Override(string batchId, [FromBody] OverrideRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Sequence is not { } sequence)
        {
            return MissingSequence();
        }

        if (request.GrantId is not { } grantId)
        {
            return Problem([Error.Validation("Request.Invalid", "grantId is required.", new Dictionary<string, object> { ["field"] = "grantId" })]);
        }

        var result = await weighing.OverrideAsync(batchId, sequence, grantId, request.Reason, OperatorBadge, cancellationToken);
        return result.Match<IActionResult>(
            r => CreatedAtAction(nameof(Get), new { batchId = r.Session.BatchId }, new ConfirmationResponse(ToResponse(r.Record), ToResponse(r.Session))),
            Problem);
    }

    /// <summary>Completes the batch (idempotent); 409 <c>Batch.Incomplete</c> while components are pending (story 013).</summary>
    [HttpPost("completion")]
    [ProducesResponseType<BatchSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Complete(string batchId, CancellationToken cancellationToken)
    {
        var result = await weighing.CompleteAsync(batchId, OperatorBadge, cancellationToken);
        return result.Match<IActionResult>(s => Ok(ToSummary(s)), Problem);
    }

    /// <summary>The batch summary (FR-18): n of N, within tolerance, total, elapsed, operators, per-chemical lines.</summary>
    [HttpGet("summary")]
    [ProducesResponseType<BatchSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Summary(string batchId, CancellationToken cancellationToken)
    {
        var result = await weighing.GetSummaryAsync(batchId, cancellationToken);
        return result.Match<IActionResult>(s => Ok(ToSummary(s)), Problem);
    }

    /// <summary>Quit (F4): discards the attempt of the named component; no record (story 012).</summary>
    [HttpPost("quit")]
    [ProducesResponseType<WeighingSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Quit(string batchId, [FromBody] ComponentCommandRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Sequence is not { } sequence)
        {
            return MissingSequence();
        }

        var result = await weighing.QuitAsync(batchId, sequence, OperatorBadge, cancellationToken);
        return result.Match<IActionResult>(s => Ok(ToResponse(s)), Problem);
    }

    /// <summary>Leaves the batch (LEAVE-DISCARDS): any attempt is discarded, every record is kept (story 012).</summary>
    [HttpPost("leave")]
    [ProducesResponseType<WeighingSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Leave(string batchId, CancellationToken cancellationToken)
    {
        var result = await weighing.LeaveAsync(batchId, OperatorBadge, cancellationToken);
        return result.Match<IActionResult>(s => Ok(ToResponse(s)), Problem);
    }

    private string OperatorBadge => StationClaims.BadgeOf(User) ?? string.Empty;

    private IActionResult MissingSequence() =>
        Problem([Error.Validation("Request.Invalid", "sequence is required.", new Dictionary<string, object> { ["field"] = "sequence" })]);

    private static WeighingSessionResponse ToResponse(WeighingSession s) => new(
        s.BatchId,
        WireNames.Of(s.Status),
        s.StartedAt,
        s.StartedBy,
        s.CompletedAt,
        s.CompletedBy,
        new ProgressResponse(s.DoneCount, s.Components.Count),
        s.Suggested?.Sequence,
        s.ActiveSequence,
        s.ReadyToComplete,
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
            c.SuggestedContainer,
            ToResponse(c.Attempt),
            c.Record is { } r ? new ComponentRecordResponse(r.Id, ToWire(r.Actual), WireNames.Of(r.Class), r.OperatorBadgeId, r.ConfirmedAt, r.IsOverride) : null);
    }

    private static AttemptResponse? ToResponse(WeighingAttempt? attempt)
    {
        if (attempt is null)
        {
            return null;
        }

        var tare = attempt.Tare is { } t ? ToWire(t.Tare) : null;
        return new AttemptResponse(attempt.Lot, attempt.Container, tare, attempt.Tare?.TakenAt);
    }

    private static WeighingRecordResponse ToResponse(WeighingRecord r) => new(
        r.Id,
        r.BatchId,
        r.Sequence,
        r.ChemicalId,
        r.ChemicalName,
        ToWire(r.Required),
        ToWire(r.Actual),
        WireNames.Of(r.Class),
        new AppliedToleranceResponse(ToWire(r.Tolerance), WireNames.Of(r.ToleranceSource)),
        r.ScaleId,
        WireNames.Of(r.ScaleClass),
        r.Lot,
        r.Container,
        ToWire(r.Tare),
        r.OperatorBadgeId,
        r.ConfirmedAt,
        ToSurplus(r.Surplus),
        r.Override is { } o ? new OverrideResponse(o.SupervisorBadgeId, o.SupervisorName, o.Reason, o.OverriddenAt) : null);

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
            r.Component.Tolerance.IsBlocked ? "blocked" : "resolved",
            new ConfirmationCheckResponse(r.Check.CanConfirm, [.. r.Check.Missing.Select(WeighingErrors.GapName)]),
            ToSurplus(classification?.Surplus),
            new OverrideOfferResponse(OverrideOffered(r.Check)));
    }

    /// <summary>The summary contract.</summary>
    private static BatchSummaryResponse ToSummary(BatchSummary s) => new(
        s.BatchId,
        s.OrderId,
        s.Machine,
        s.Shade,
        WireNames.Of(s.Status),
        s.Done,
        s.Total,
        s.AllWithinTolerance,
        s.OverrideCount,
        new QuantityResponse(Math.Round(s.TotalDispensed.Value, 3, MidpointRounding.AwayFromZero), s.TotalDispensed.Symbol),
        s.StartedAt,
        s.CompletedAt,
        s.ElapsedSeconds,
        s.Operators,
        WriteBackController.ToResponse(s.Orgatex),
        TicketController.ToState(s.Ticket),
        [.. s.Lines.Select(l => new BatchSummaryLineResponse(
            l.Sequence, l.ChemicalId, l.ChemicalName, ToWire(l.Required), l.Actual is { } a ? ToWire(a) : null, LineStatus(l), l.Override))]);

    private static string LineStatus(BatchSummaryLine line) => line.Class is { } c ? WireNames.Of(c) : WireNames.Of(line.State);

    private static SurplusResponse? ToSurplus(Surplus? surplus) =>
        surplus is null ? null : new SurplusResponse(ToWire(surplus.Amount), surplus.PercentOfTarget);

    /// <summary>Override is offered only when tolerance is the one missing condition (A11, PD-05 option 1).</summary>
    private static bool OverrideOffered(ConfirmationCheck check) =>
        check.Classification is { IsConfirmable: false } && check.Missing.SequenceEqual([ConfirmationGap.InTolerance]);

    private static string? SourceOf(ComponentWeighing c) => c.ToleranceSource is { } source ? WireNames.Of(source) : null;

    private static QuantityResponse ToWire(Quantity q) => new(q.Value, q.Symbol);
}
