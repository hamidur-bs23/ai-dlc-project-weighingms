namespace WeighingMS.Api.Contracts.Weighing;

/// <summary>A command on the component being weighed (A8: always names the component).</summary>
public sealed record ComponentCommandRequest(int? Sequence);

/// <summary>Capture the lot for the active component.</summary>
public sealed record CaptureLotRequest(int? Sequence, string? Lot);

/// <summary>Capture the container for the active component.</summary>
public sealed record CaptureContainerRequest(int? Sequence, string? Container);
