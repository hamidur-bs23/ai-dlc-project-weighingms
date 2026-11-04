namespace WeighingMS.Simulators.Host.Scanner;

/// <summary>A queued scan. <see cref="Sequence"/> increases by one per scan, starting at 1 (training contract).</summary>
public sealed record ScanResponse(long Sequence, string Code, DateTimeOffset ScannedAt);
