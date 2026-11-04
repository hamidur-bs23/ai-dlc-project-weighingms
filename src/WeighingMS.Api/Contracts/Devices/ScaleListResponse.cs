namespace WeighingMS.Api.Contracts.Devices;

/// <summary>The scales and the live-weight poll interval the UI should use (ADR-004).</summary>
public sealed record ScaleListResponse(IReadOnlyList<ScaleResponse> Items, int PollIntervalMs);
