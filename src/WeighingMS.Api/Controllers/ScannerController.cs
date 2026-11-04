using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeighingMS.Api.Common;
using WeighingMS.Api.Contracts.Devices;
using WeighingMS.Application.Devices;

namespace WeighingMS.Api.Controllers;

/// <summary>Scanner through <see cref="IScanner"/>: scans are collected with a cursor (each delivered once).</summary>
[Route("api/scanner")]
[Authorize(Policy = StationPolicies.RequireOperator)]
public sealed class ScannerController(DeviceService devices) : ApiControllerBase
{
    /// <summary>Scans newer than <paramref name="after"/>; the response's <c>cursor</c> is the next <c>after</c>.</summary>
    [HttpGet("scans")]
    [ProducesResponseType<ScanListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Scans([FromQuery] long after, CancellationToken cancellationToken)
    {
        var result = await devices.GetScansAsync(after, cancellationToken);
        return result.Match<IActionResult>(
            r => Ok(new ScanListResponse([.. r.Items.Select(s => new ScanResponse(s.Sequence, s.Code, s.ScannedAt))], r.Cursor)),
            Problem);
    }

    /// <summary>Scanner availability.</summary>
    [HttpGet("status")]
    [ProducesResponseType<DeviceStatusResponse>(StatusCodes.Status200OK)]
    public async Task<DeviceStatusResponse> Status(CancellationToken cancellationToken)
    {
        var (status, since) = await devices.GetScannerStatusAsync(cancellationToken);
        return new DeviceStatusResponse(DeviceService.ScannerDevice, WireNames.Of(status), since);
    }
}
