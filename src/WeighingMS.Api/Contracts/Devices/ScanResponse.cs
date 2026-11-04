namespace WeighingMS.Api.Contracts.Devices;

/// <summary>One scan.</summary>
public sealed record ScanResponse(long Sequence, string Code, DateTimeOffset ScannedAt);
