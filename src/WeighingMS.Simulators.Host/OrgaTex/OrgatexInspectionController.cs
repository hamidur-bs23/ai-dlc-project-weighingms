using Microsoft.AspNetCore.Mvc;
using WeighingMS.Simulators.Host.Common;

namespace WeighingMS.Simulators.Host.OrgaTex;

/// <summary>Simulation control: inspect what the dummy OrgaTEX received (evidence for STEP 26).</summary>
[ApiController]
[SimulationControl]
[Route("_sim/orgatex")]
[Produces("application/json")]
public sealed class OrgatexInspectionController(ConsumptionStore consumption) : ControllerBase
{
    /// <summary>Every received consumption post, once each.</summary>
    [HttpGet("consumption")]
    [ProducesResponseType<IReadOnlyList<ConsumptionReceipt>>(StatusCodes.Status200OK)]
    public IReadOnlyList<ConsumptionReceipt> GetConsumption() => consumption.All();
}
