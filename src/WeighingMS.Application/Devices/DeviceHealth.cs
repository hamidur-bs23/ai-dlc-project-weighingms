namespace WeighingMS.Application.Devices;

/// <summary>Status of one device or integration, and since when it has had that status.</summary>
public sealed record DeviceHealth(string Device, DeviceKind Kind, DeviceStatus Status, DateTimeOffset Since);
