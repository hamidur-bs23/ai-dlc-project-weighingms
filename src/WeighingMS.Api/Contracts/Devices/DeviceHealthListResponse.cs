namespace WeighingMS.Api.Contracts.Devices;

/// <summary>
/// Health of every scale, the scanner, the printer and OrgaTEX, and how many OrgaTEX write-backs are not yet sent
/// (pending or sync failed; the status strip's unsynced count, bolt 011).
/// </summary>
public sealed record DeviceHealthListResponse(IReadOnlyList<DeviceHealthResponse> Items, int UnsyncedWriteBacks);
