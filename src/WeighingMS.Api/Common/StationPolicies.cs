namespace WeighingMS.Api.Common;

/// <summary>Authorization policy names (system-architecture → Security Patterns).</summary>
public static class StationPolicies
{
    /// <summary>Any signed-in station user (operators and supervisors can both operate the station).</summary>
    public const string RequireOperator = "RequireOperator";

    /// <summary>A signed-in supervisor.</summary>
    public const string RequireSupervisor = "RequireSupervisor";

    /// <summary>Rate-limit policy for sign-in and step-up.</summary>
    public const string AuthRateLimit = "auth";
}
