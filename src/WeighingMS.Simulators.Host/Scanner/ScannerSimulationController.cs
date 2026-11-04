using Microsoft.AspNetCore.Mvc;
using WeighingMS.Simulators.Host.Common;

namespace WeighingMS.Simulators.Host.Scanner;

/// <summary>Simulation control: make the dummy scanner read a barcode.</summary>
[ApiController]
[SimulationControl]
[Route("_sim/scanner")]
[Produces("application/json")]
public sealed class ScannerSimulationController(ScanBuffer queue) : ControllerBase
{
    /// <summary>Emits a scan; it is queued for WeighingMS to collect.</summary>
    [HttpPost("scan")]
    [ProducesResponseType<ScanResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult Scan([FromBody] ScanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Accepted(queue.Enqueue(request.Code));
    }
}
