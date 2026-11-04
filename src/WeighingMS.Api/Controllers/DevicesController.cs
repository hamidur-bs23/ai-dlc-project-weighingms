using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeighingMS.Api.Common;
using WeighingMS.Api.Contracts.Devices;
using WeighingMS.Application.Devices;

namespace WeighingMS.Api.Controllers;

/// <summary>Aggregated device health for the status strip (scales, scanner, printer, OrgaTEX).</summary>
[Route("api/devices")]
[Authorize(Policy = StationPolicies.RequireOperator)]
public sealed class DevicesController(DeviceService devices) : ApiControllerBase
{
    /// <summary>Status and since-time of every device and integration.</summary>
    [HttpGet("health")]
    [ProducesResponseType<DeviceHealthListResponse>(StatusCodes.Status200OK)]
    public async Task<DeviceHealthListResponse> Health(CancellationToken cancellationToken)
    {
        var health = await devices.GetHealthAsync(cancellationToken);
        return new DeviceHealthListResponse(
            [.. health.Select(h => new DeviceHealthResponse(h.Device, WireNames.Of(h.Kind), WireNames.Of(h.Status), h.Since))]);
    }
}
