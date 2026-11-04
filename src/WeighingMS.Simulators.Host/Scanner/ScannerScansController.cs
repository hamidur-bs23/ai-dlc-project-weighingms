using Microsoft.AspNetCore.Mvc;
using WeighingMS.Simulators.Host.Common;
using WeighingMS.Simulators.Host.Faults;

namespace WeighingMS.Simulators.Host.Scanner;

/// <summary>
/// Dummy Scanner scan collection (device-style HTTP, PD-03; no keyboard wedge). Training contract only:
/// WeighingMS collects scans newer than its cursor.
/// </summary>
[ApiController]
[Route("api/scanner/scans")]
[Produces("application/json")]
[FaultInjection(FaultTargets.Scanner)]
public sealed class ScannerScansController(ScanBuffer scans) : ControllerBase
{
    /// <summary>Scans newer than <paramref name="after"/> (the last sequence the caller has seen; 0 for all).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ScanResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult GetScans([FromQuery] long after = 0) =>
        after < 0
            ? SimProblems.Create(StatusCodes.Status400BadRequest, "Scanner.InvalidCursor", "Cursor must be zero or positive")
            : Ok(scans.After(after));
}
