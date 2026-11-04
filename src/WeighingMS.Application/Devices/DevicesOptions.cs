namespace WeighingMS.Application.Devices;

/// <summary>Device configuration (section <c>Devices</c>).</summary>
public sealed class DevicesOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Devices";

    /// <summary>How often the UI polls the live weight (ADR-004, default 250 ms).</summary>
    public TimeSpan ScalePollInterval { get; set; } = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Per-scale zero band in the scale's unit: a reading below <c>−band</c> is invalid (below zero). A scale without
    /// an entry has a zero band of 0.
    /// </summary>
    public Dictionary<string, decimal> ZeroBands { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
