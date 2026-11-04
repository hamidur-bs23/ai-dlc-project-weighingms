using System.ComponentModel.DataAnnotations;

namespace WeighingMS.Simulators.Host.OrgaTex;

/// <summary>Actual consumption posted for a completed batch (training contract).</summary>
public sealed record ConsumptionRequest(
    [Required, MinLength(1)] IReadOnlyList<ConsumptionLineRequest> Lines);
