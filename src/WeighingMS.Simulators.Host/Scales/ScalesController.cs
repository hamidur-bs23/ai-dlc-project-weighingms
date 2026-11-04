using Microsoft.AspNetCore.Mvc;
using WeighingMS.Simulators.Host.Common;

namespace WeighingMS.Simulators.Host.Scales;

/// <summary>
/// Dummy Scale API (workbook STEP 16): list, status, weight and tare.
/// Training contracts only: not claims about the real scale protocol.
/// </summary>
[ApiController]
[Route("api/scales")]
[Produces("application/json")]
public sealed class ScalesController(ScaleSimulator scales, TimeProvider clock) : ControllerBase
{
    /// <summary>All station scales.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ScaleResponse>>(StatusCodes.Status200OK)]
    public IReadOnlyList<ScaleResponse> GetScales() =>
        [.. scales.Scales.Select(s => new ScaleResponse(s.ScaleId, s.Name, s.ScaleClass, s.Unit, s.Capacity, s.Resolution, ScaleSimulator.Ready))];

    /// <summary>Scale availability.</summary>
    [HttpGet("{scaleId}/status")]
    [ProducesResponseType<ScaleStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult GetStatus(string scaleId)
    {
        var scale = scales.Find(scaleId);
        return scale is null
            ? SimProblems.ScaleNotFound(scaleId)
            : Ok(new ScaleStatusResponse(scale.ScaleId, ScaleSimulator.Ready, clock.GetUtcNow()));
    }

    /// <summary>Current net weight.</summary>
    [HttpGet("{scaleId}/weight")]
    [ProducesResponseType<WeightReadingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult GetWeight(string scaleId) =>
        scales.Find(scaleId) is null ? SimProblems.ScaleNotFound(scaleId) : Ok(scales.Read(scaleId));

    /// <summary>Tares the scale.</summary>
    [HttpPost("{scaleId}/tare")]
    [ProducesResponseType<TareResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult Tare(string scaleId) =>
        scales.Find(scaleId) is null ? SimProblems.ScaleNotFound(scaleId) : Ok(scales.Tare(scaleId));
}
