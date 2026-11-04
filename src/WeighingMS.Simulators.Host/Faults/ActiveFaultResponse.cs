namespace WeighingMS.Simulators.Host.Faults;

/// <summary>An active fault (simulation control).</summary>
public sealed record ActiveFaultResponse(string Target, FaultMode Mode, DateTimeOffset? ExpiresAt);
