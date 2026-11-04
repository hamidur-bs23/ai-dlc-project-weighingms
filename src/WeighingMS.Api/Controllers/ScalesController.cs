using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeighingMS.Api.Common;
using WeighingMS.Api.Contracts.Devices;
using WeighingMS.Application.Devices;

namespace WeighingMS.Api.Controllers;

/// <summary>
/// Station scales through <see cref="IScaleDevice"/> (workbook STEP 16/21 paths preserved). The UI calls only these
/// endpoints, never the device.
/// </summary>
[Route("api/scales")]
[Authorize(Policy = StationPolicies.RequireOperator)]
public sealed class ScalesController(DeviceService devices) : ApiControllerBase
{
    /// <summary>All scales with status, and the UI poll interval.</summary>
    [HttpGet]
    [ProducesResponseType<ScaleListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await devices.ListScalesAsync(cancellationToken);
        return result.Match<IActionResult>(
            list => Ok(new ScaleListResponse(
                [.. list.Select(x => new ScaleResponse(
                    x.Scale.ScaleId, x.Scale.Name, x.Scale.ScaleClass, ToWire(x.Scale.Capacity), ToWire(x.Scale.Resolution), WireNames.Of(x.Status)))],
                (int)devices.PollInterval.TotalMilliseconds)),
            Problem);
    }

    /// <summary>Availability of one scale.</summary>
    [HttpGet("{scaleId}/status")]
    [ProducesResponseType<DeviceStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Status(string scaleId, CancellationToken cancellationToken)
    {
        var result = await devices.GetScaleStatusAsync(scaleId, cancellationToken);
        return result.Match<IActionResult>(s => Ok(new DeviceStatusResponse(scaleId, WireNames.Of(s.Status), s.Since)), Problem);
    }

    /// <summary>The live net weight with its validity.</summary>
    [HttpGet("{scaleId}/weight")]
    [ProducesResponseType<WeightResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Weight(string scaleId, CancellationToken cancellationToken)
    {
        var result = await devices.ReadAsync(scaleId, cancellationToken);
        return result.Match<IActionResult>(
            r => Ok(new WeightResponse(r.ScaleId, r.Net is { } n ? ToWire(n) : null, r.Stable, WireNames.Of(r.Validity), r.ReadAt, r.OnlineSince)),
            Problem);
    }

    /// <summary>Tares the scale.</summary>
    [HttpPost("{scaleId}/tare")]
    [ProducesResponseType<TareResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Tare(string scaleId, CancellationToken cancellationToken)
    {
        var result = await devices.TareAsync(scaleId, cancellationToken);
        return result.Match<IActionResult>(t => Ok(new TareResponse(t.ScaleId, ToWire(t.Tare), t.TaredAt)), Problem);
    }

    private static QuantityResponse ToWire(DeviceQuantity q) => new(q.Value, q.Unit);
}
