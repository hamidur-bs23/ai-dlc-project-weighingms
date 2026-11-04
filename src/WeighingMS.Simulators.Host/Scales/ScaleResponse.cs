namespace WeighingMS.Simulators.Host.Scales;

/// <summary>A station scale (training contract).</summary>
public sealed record ScaleResponse(
    string ScaleId,
    string Name,
    string ScaleClass,
    string Unit,
    decimal Capacity,
    decimal Resolution,
    string Status);
