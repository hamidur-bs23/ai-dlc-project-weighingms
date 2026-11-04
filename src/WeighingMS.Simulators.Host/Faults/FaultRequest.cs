using System.ComponentModel.DataAnnotations;

namespace WeighingMS.Simulators.Host.Faults;

/// <summary>Injects a fault: <c>target</c> is orgatex, scale:{scaleId}, printer or scanner.</summary>
public sealed record FaultRequest(
    [Required(AllowEmptyStrings = false)] string Target,
    [Required] FaultMode? Mode,
    [Range(1, 86_400_000)] int? DurationMs);
