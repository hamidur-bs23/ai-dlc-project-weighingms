using Microsoft.AspNetCore.Mvc;
using WeighingMS.Simulators.Host.Common;
using WeighingMS.Simulators.Host.Scales;

namespace WeighingMS.Simulators.Host.Faults;

/// <summary>Simulation control: inject, list and clear faults (STEP 30 failure scenarios).</summary>
[ApiController]
[SimulationControl]
[Route("_sim/faults")]
[Produces("application/json")]
public sealed class FaultsController(FaultRegistry faults, ScaleSimulator scales) : ControllerBase
{
    /// <summary>Sets or replaces the fault on a target.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult Inject([FromBody] FaultRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var target = FaultTargets.Normalise(request.Target);
        if (!IsKnownTarget(target))
        {
            return SimProblems.Create(
                StatusCodes.Status400BadRequest,
                "Fault.UnknownTarget",
                "Unknown fault target",
                $"'{request.Target}' is not orgatex, printer, scanner or scale:{{scaleId}} of a known scale.");
        }

        var duration = request.DurationMs is { } ms ? TimeSpan.FromMilliseconds(ms) : (TimeSpan?)null;
        faults.Set(target, request.Mode!.Value, duration);
        return NoContent();
    }

    /// <summary>The active faults.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ActiveFaultResponse>>(StatusCodes.Status200OK)]
    public IReadOnlyList<ActiveFaultResponse> List() => faults.All();

    /// <summary>Clears every fault.</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ClearAll()
    {
        faults.Clear();
        return NoContent();
    }

    private bool IsKnownTarget(string target) =>
        target is FaultTargets.OrgaTex or FaultTargets.Printer or FaultTargets.Scanner
        || (target.StartsWith(FaultTargets.ScalePrefix, StringComparison.Ordinal)
            && scales.Find(target[FaultTargets.ScalePrefix.Length..]) is not null);
}
