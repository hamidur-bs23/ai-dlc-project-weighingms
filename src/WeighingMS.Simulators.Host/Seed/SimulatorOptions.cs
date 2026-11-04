using System.ComponentModel.DataAnnotations;

namespace WeighingMS.Simulators.Host.Seed;

/// <summary>Simulator configuration (section <c>Simulator</c>).</summary>
public sealed class SimulatorOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Simulator";

    /// <summary>Path of the seed file, relative to the content root.</summary>
    [Required]
    public string SeedPath { get; set; } = "Seed/seed.json";

    /// <summary>Duration of a simulated pour from empty to its landing value.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:10:00")]
    public TimeSpan PourDuration { get; set; } = TimeSpan.FromSeconds(8);
}
