using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeighingMS.Api.Common;
using WeighingMS.Api.Contracts.Weighing;
using WeighingMS.Application.Tickets;

namespace WeighingMS.Api.Controllers;

/// <summary>A completed batch's ticket (bolt 012): read it, and print it. Every print after the first is a reprint.</summary>
[Route("api/batches/{batchId}/session/ticket")]
[Authorize(Policy = StationPolicies.RequireOperator)]
public sealed class TicketController(TicketService tickets) : ApiControllerBase
{
    /// <summary>The ticket; 404 <c>Ticket.NotFound</c> when the batch has none.</summary>
    [HttpGet]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string batchId, CancellationToken cancellationToken)
    {
        var result = await tickets.GetAsync(batchId, cancellationToken);
        return result.Match<IActionResult>(v => Ok(ToResponse(v)), Problem);
    }

    /// <summary>
    /// Prints the ticket. 409 <c>Ticket.AlreadyPrinted</c> for a stale press; 503 <c>Printer.Unavailable</c> when the printer
    /// is down (the ticket stays as it was, and pressing again later is safe).
    /// </summary>
    [HttpPost("print")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Print(string batchId, [FromBody] PrintTicketRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ExpectedPrintCount is not { } expected)
        {
            return Problem([Error.Validation("expectedPrintCount", "expectedPrintCount is required.")]);
        }

        var result = await tickets.PrintAsync(batchId, expected, StationClaims.BadgeOf(User) ?? string.Empty, cancellationToken);
        return result.Match<IActionResult>(v => Ok(ToResponse(v)), Problem);
    }

    /// <summary>The summary's ticket state (shared with the batch summary).</summary>
    internal static TicketStateResponse ToState(TicketView? view) =>
        view is null ? TicketStateResponse.NotAvailable : new TicketStateResponse(WireNames.Of(view.Status), view.PrintCount);

    private static TicketResponse ToResponse(TicketView v) =>
        new(v.BatchId, WireNames.Of(v.Status), v.PrintCount, v.Lines, new TicketBarcodeResponse(v.BarcodeSymbology, v.BarcodeValue), v.FirstPrintedAt, v.LastPrintedAt);
}
