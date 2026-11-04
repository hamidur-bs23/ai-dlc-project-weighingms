namespace WeighingMS.Simulators.Host.Faults;

/// <summary>How a faulted dummy misbehaves (api-conventions <c>/_sim/faults</c>).</summary>
public enum FaultMode
{
    /// <summary>503 ProblemDetails; the device or OrgaTEX status reports OFFLINE.</summary>
    Unavailable,

    /// <summary>The response is held for <c>FaultTimeout</c>, then answered with 504.</summary>
    Timeout,

    /// <summary>500 ProblemDetails.</summary>
    Error500,

    /// <summary>The response is delayed by <c>SlowDelay</c>, then served normally.</summary>
    Slow,
}
