namespace WeighingMS.Application.Devices;

/// <summary>
/// A reading as the device sent it. <see cref="Net"/> or <see cref="Unit"/> is null when the device sent no usable
/// value (malformed).
/// </summary>
public sealed record RawScaleReading(string ScaleId, decimal? Net, string? Unit, bool Stable, bool OverCapacity, DateTimeOffset ReadAt);
