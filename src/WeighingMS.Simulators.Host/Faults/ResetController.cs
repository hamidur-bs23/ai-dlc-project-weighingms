using Microsoft.AspNetCore.Mvc;
using WeighingMS.Simulators.Host.Common;
using WeighingMS.Simulators.Host.OrgaTex;
using WeighingMS.Simulators.Host.Printer;
using WeighingMS.Simulators.Host.Scales;
using WeighingMS.Simulators.Host.Scanner;

namespace WeighingMS.Simulators.Host.Faults;

/// <summary>
/// Simulation control: return every dummy to its start state. The seed file is immutable during a run,
/// so a reset clears what changed since start-up: scale states and tares, consumption, scans, prints and faults.
/// </summary>
[ApiController]
[SimulationControl]
[Route("_sim/reset")]
public sealed class ResetController(
    ScaleSimulator scales,
    ConsumptionStore consumption,
    ScanBuffer scanner,
    PrintStore prints,
    FaultRegistry faults) : ControllerBase
{
    /// <summary>Resets all dummies.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Reset()
    {
        scales.Reset();
        consumption.Reset();
        scanner.Reset();
        prints.Reset();
        faults.Clear();
        return NoContent();
    }
}
