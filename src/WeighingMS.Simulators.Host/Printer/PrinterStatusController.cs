using Microsoft.AspNetCore.Mvc;
using WeighingMS.Simulators.Host.Common;
using WeighingMS.Simulators.Host.Faults;

namespace WeighingMS.Simulators.Host.Printer;

/// <summary>Dummy Printer status (training contract).</summary>
[ApiController]
[Route("api/printer")]
[Produces("application/json")]
public sealed class PrinterStatusController(FaultRegistry faults, TimeProvider clock) : ControllerBase
{
    /// <summary>READY, or OFFLINE while an <c>unavailable</c> fault is active.</summary>
    [HttpGet("status")]
    [ProducesResponseType<DeviceStatusResponse>(StatusCodes.Status200OK)]
    public DeviceStatusResponse GetStatus() =>
        new(FaultTargets.Printer, DeviceStatus.From(faults.IsUnavailable(FaultTargets.Printer)), clock.GetUtcNow());
}
