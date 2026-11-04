namespace WeighingMS.Simulators.Host.Seed;

/// <summary>
/// A simulated scale. <see cref="Capacity"/> is checked against the net reading (training simplification);
/// <see cref="ContainerTare"/> is the empty-container weight placed on the scale before pouring.
/// </summary>
public sealed record SeedScale(
    string ScaleId,
    string Name,
    string ScaleClass,
    string Unit,
    decimal Capacity,
    decimal Resolution,
    decimal ContainerTare);
