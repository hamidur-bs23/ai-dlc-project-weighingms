namespace WeighingMS.Domain.Weighing;

/// <summary>What has been captured so far for the active component (lot, container, tare). Never a record.</summary>
public sealed record WeighingAttempt(DateTimeOffset? StartedAt, string? StartedBy, string? Lot, string? Container, TareTaken? Tare);
