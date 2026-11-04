namespace WeighingMS.Simulators.Host.Common;

/// <summary>READY / OFFLINE status of a dummy device (training contract).</summary>
public sealed record DeviceStatusResponse(string Device, string Status, DateTimeOffset Timestamp);
