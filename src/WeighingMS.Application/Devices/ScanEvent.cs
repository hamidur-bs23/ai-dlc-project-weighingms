namespace WeighingMS.Application.Devices;

/// <summary>One scan read by the scanner; <see cref="Sequence"/> is the collection cursor.</summary>
public sealed record ScanEvent(long Sequence, string Code, DateTimeOffset ScannedAt);
