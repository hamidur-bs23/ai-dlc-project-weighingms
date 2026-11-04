namespace WeighingMS.Simulators.Host.Scales;

/// <summary>The tare value the scale stored (training contract).</summary>
public sealed record TareResponse(string ScaleId, decimal Tare, string Unit, DateTimeOffset Timestamp);
