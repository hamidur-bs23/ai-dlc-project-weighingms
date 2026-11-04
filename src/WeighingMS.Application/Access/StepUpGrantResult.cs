namespace WeighingMS.Application.Access;

/// <summary>An issued step-up grant, returned to the client.</summary>
public sealed record StepUpGrantResult(Guid GrantId, string SupervisorBadgeId, string SupervisorName, string Action, DateTimeOffset ExpiresAt);
