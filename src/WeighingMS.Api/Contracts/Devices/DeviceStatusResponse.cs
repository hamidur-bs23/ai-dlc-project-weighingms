namespace WeighingMS.Api.Contracts.Devices;

/// <summary>Availability of one device and since when.</summary>
public sealed record DeviceStatusResponse(string Device, string Status, DateTimeOffset Since);
