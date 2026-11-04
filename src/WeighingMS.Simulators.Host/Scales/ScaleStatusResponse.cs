namespace WeighingMS.Simulators.Host.Scales;

/// <summary>Scale availability: READY or OFFLINE (training contract).</summary>
public sealed record ScaleStatusResponse(string ScaleId, string Status, DateTimeOffset Timestamp);
