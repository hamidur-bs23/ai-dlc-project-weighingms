namespace WeighingMS.Infrastructure.Integrations;

/// <summary>Where the device and OrgaTEX adapters connect (section <c>Integrations:Simulators</c>).</summary>
public sealed class SimulatorIntegrationOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Integrations:Simulators";

    /// <summary>Base URL of WeighingMS.Simulators.Host (the dummies) or, later, the real systems' gateway.</summary>
    public Uri BaseUrl { get; set; } = new("http://localhost:5090");

    /// <summary>Timeout of one HTTP attempt to a device.</summary>
    public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Minimum calls in the sampling window before the circuit breaker can open.</summary>
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;

    /// <summary>How long the circuit stays open before a trial call.</summary>
    public TimeSpan CircuitBreakerBreakDuration { get; set; } = TimeSpan.FromSeconds(5);
}
