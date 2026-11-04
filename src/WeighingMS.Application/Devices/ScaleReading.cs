namespace WeighingMS.Application.Devices;

/// <summary>
/// A classified scale reading. <see cref="OnlineSince"/> is when the scale last came back from offline: a tare taken
/// before it no longer counts (used by the weighing session, bolt 008).
/// </summary>
public sealed record ScaleReading(
    string ScaleId,
    DeviceQuantity? Net,
    bool Stable,
    ReadingValidity Validity,
    DateTimeOffset ReadAt,
    DateTimeOffset? OnlineSince);
