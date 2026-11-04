using System.Text.Json.Serialization;

namespace WeighingMS.Simulators.Host.Scales;

/// <summary>A quantity with its unit (g or kg).</summary>
public sealed record QuantityDto([property: JsonRequired] decimal Value, string Unit);
