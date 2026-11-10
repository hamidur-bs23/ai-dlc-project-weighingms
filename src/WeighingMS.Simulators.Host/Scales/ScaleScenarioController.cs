using Microsoft.AspNetCore.Mvc;
using WeighingMS.Simulators.Host.Common;

namespace WeighingMS.Simulators.Host.Scales;

/// <summary>Simulation control: choose the weighing scenario a dummy scale plays.</summary>
[ApiController]
[SimulationControl]
[Route("_sim/scales")]
[Produces("application/json")]
public sealed class ScaleScenarioController(ScaleSimulator scales) : ControllerBase
{
    /// <summary>Starts a scenario on a scale.</summary>
    [HttpPost("{scaleId}/scenario")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult SetScenario(string scaleId, [FromBody] ScenarioRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (scales.Find(scaleId) is null)
        {
            return SimProblems.ScaleNotFound(scaleId);
        }

        var error = scales.SetScenario(scaleId, request.Scenario!.Value, request.Target, request.Tolerance);
        return error is null
            ? NoContent()
            : SimProblems.Create(StatusCodes.Status400BadRequest, "Scenario.Invalid", "Invalid scenario request", error);
    }

    /// <summary>
    /// Takes the filled container off the scale and puts an empty one on (between components). The tare stays until the
    /// operator tares again.
    /// </summary>
    [HttpPost("{scaleId}/container")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult PlaceEmptyContainer(string scaleId)
    {
        if (scales.Find(scaleId) is null)
        {
            return SimProblems.ScaleNotFound(scaleId);
        }

        scales.PlaceEmptyContainer(scaleId);
        return NoContent();
    }
}
