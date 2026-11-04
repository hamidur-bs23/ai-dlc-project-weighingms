using Microsoft.AspNetCore.Mvc;
using WeighingMS.Simulators.Host.Common;

namespace WeighingMS.Simulators.Host.Printer;

/// <summary>Simulation control: inspect what the dummy printer printed (evidence for STEP 27).</summary>
[ApiController]
[SimulationControl]
[Route("_sim/printer")]
[Produces("application/json")]
public sealed class PrinterInspectionController(PrintStore prints) : ControllerBase
{
    /// <summary>Every printed ticket, once each, with its content.</summary>
    [HttpGet("tickets")]
    [ProducesResponseType<IReadOnlyList<PrintReceipt>>(StatusCodes.Status200OK)]
    public IReadOnlyList<PrintReceipt> GetTickets() => prints.All();
}
