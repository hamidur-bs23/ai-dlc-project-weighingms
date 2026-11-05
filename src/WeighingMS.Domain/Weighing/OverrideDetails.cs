namespace WeighingMS.Domain.Weighing;

/// <summary>The authorised exception on a record (FR-15, OVR): who authorised it, why, when and with which grant.</summary>
public sealed record OverrideDetails(Guid GrantId, string SupervisorBadgeId, string SupervisorName, string Reason, DateTimeOffset OverriddenAt);

/// <summary>A supervisor's step-up approval as the weighing aggregate needs it (ADR-009).</summary>
public sealed record OverrideAuthority(Guid GrantId, string SupervisorBadgeId, string SupervisorName);
