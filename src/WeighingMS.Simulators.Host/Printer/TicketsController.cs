using Microsoft.AspNetCore.Mvc;
using WeighingMS.Simulators.Host.Common;
using WeighingMS.Simulators.Host.Faults;

namespace WeighingMS.Simulators.Host.Printer;

/// <summary>Dummy Printer (workbook STEP 16): accepts tickets. Training contract only.</summary>
[ApiController]
[Route("api/tickets")]
[Produces("application/json")]
[FaultInjection(FaultTargets.Printer)]
public sealed class TicketsController(PrintStore prints) : ControllerBase
{
    /// <summary>Name of the idempotency header required on prints.</summary>
    public const string IdempotencyHeader = "Idempotency-Key";

    /// <summary>Prints a ticket. A repeated <c>Idempotency-Key</c> returns the original receipt and prints nothing.</summary>
    [HttpPost]
    [ProducesResponseType<PrintReceipt>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult Print([FromHeader(Name = IdempotencyHeader)] string? idempotencyKey, [FromBody] TicketRequest ticket)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return SimProblems.Create(StatusCodes.Status400BadRequest, "Printer.IdempotencyKeyMissing", "Idempotency-Key header is required");
        }

        var (_, receipt) = prints.Print(idempotencyKey.Trim(), ticket);
        return StatusCode(StatusCodes.Status201Created, receipt);
    }
}
