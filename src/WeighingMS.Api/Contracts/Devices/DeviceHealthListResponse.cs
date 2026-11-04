namespace WeighingMS.Api.Contracts.Devices;

/// <summary>Health of every scale, the scanner, the printer and OrgaTEX.</summary>
public sealed record DeviceHealthListResponse(IReadOnlyList<DeviceHealthResponse> Items);
