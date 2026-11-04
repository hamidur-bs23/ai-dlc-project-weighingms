namespace WeighingMS.Api.Contracts;

/// <summary>A single-use supervisor grant.</summary>
public sealed record StepUpResponse(Guid GrantId, string SupervisorBadgeId, string SupervisorName, string Action, DateTimeOffset ExpiresAt);
