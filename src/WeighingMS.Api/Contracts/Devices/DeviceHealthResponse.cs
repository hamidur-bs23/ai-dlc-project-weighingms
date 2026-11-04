namespace WeighingMS.Api.Contracts.Devices;

/// <summary>One entry of the status strip.</summary>
public sealed record DeviceHealthResponse(string Device, string Kind, string Status, DateTimeOffset Since);
