namespace WeighingMS.Api.Contracts.Devices;

/// <summary>Scans newer than the requested cursor; pass <c>cursor</c> as <c>after</c> on the next poll.</summary>
public sealed record ScanListResponse(IReadOnlyList<ScanResponse> Items, long Cursor);
