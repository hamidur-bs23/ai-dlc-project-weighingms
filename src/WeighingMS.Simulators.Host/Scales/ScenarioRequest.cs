using System.ComponentModel.DataAnnotations;

namespace WeighingMS.Simulators.Host.Scales;

/// <summary>
/// Sets the scenario of a dummy scale. <see cref="Tolerance"/> is in the target's unit and is required
/// for <c>belowRange</c>, <c>enterRange</c> and <c>overshoot</c>.
/// </summary>
public sealed record ScenarioRequest(
    [Required] ScaleScenario? Scenario,
    QuantityDto? Target,
    decimal? Tolerance);
