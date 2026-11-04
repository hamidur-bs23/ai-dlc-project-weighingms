namespace WeighingMS.Application.Access;

/// <summary>Supervisor authorisation for one action, requested at the operator's station.</summary>
public sealed record StepUpCommand(string OperatorBadgeId, string SupervisorBadgeId, string Pin, string Action);
