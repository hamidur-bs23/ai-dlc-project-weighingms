using Microsoft.AspNetCore.Mvc;
using WeighingMS.Simulators.Host.Common;
using WeighingMS.Simulators.Host.Faults;

namespace WeighingMS.Simulators.Host.Scanner;

/// <summary>Dummy Scanner status (training contract).</summary>
[ApiController]
[Route("api/scanner")]
[Produces("application/json")]
public sealed class ScannerStatusController(FaultRegistry faults, TimeProvider clock) : ControllerBase
{
    /// <summary>READY, or OFFLINE while an <c>unavailable</c> fault is active.</summary>
    [HttpGet("status")]
    [ProducesResponseType<DeviceStatusResponse>(StatusCodes.Status200OK)]
    public DeviceStatusResponse GetStatus() =>
        new(FaultTargets.Scanner, DeviceStatus.From(faults.IsUnavailable(FaultTargets.Scanner)), clock.GetUtcNow());
}
