namespace WeighingMS.Api.Contracts;

/// <summary>Supervisor badge + PIN for one action (for example <c>override</c>).</summary>
public sealed record StepUpRequest(string BadgeId, string Pin, string Action);
