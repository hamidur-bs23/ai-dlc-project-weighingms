using ErrorOr;
using FluentValidation;
using WeighingMS.Application.Abstractions;
using WeighingMS.Domain.Access;
using WeighingMS.Domain.Audit;

namespace WeighingMS.Application.Access;

/// <summary>
/// Station access use cases: sign in, sign out and supervisor step-up (ADR-009). Every outcome that matters
/// is audited in the same unit of work as the change it records.
/// </summary>
public sealed class StationAccessService(
    IStationUserRepository users,
    IStepUpGrantStore grants,
    IPinHasher pinHasher,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IPlantClock clock,
    StationAccessOptions options,
    IValidator<SignInCommand> signInValidator,
    IValidator<StepUpCommand> stepUpValidator)
{
    /// <summary>Signs in an operator or supervisor by badge.</summary>
    public async Task<ErrorOr<SessionInfo>> SignInAsync(SignInCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var validation = await signInValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToErrors();
        }

        var user = await users.FindByBadgeAsync(StationUser.Normalise(command.BadgeId), cancellationToken);
        if (user is null)
        {
            return AccessErrors.BadgeNotRecognised;
        }

        var now = clock.UtcNow;
        if (command.CurrentBadgeId is { } replaced)
        {
            audit.Append(AuditEntry.Record(AuditEventType.SessionReplaced, user.BadgeId, now, subject: replaced));
        }

        audit.Append(AuditEntry.Record(AuditEventType.SignIn, user.BadgeId, now, subject: options.StationId));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var shift = string.IsNullOrWhiteSpace(command.Shift) ? null : command.Shift.Trim();
        return new SessionInfo(user.Id, user.BadgeId, user.DisplayName, user.Role, shift, options.StationId, clock.TimeZoneId, now);
    }

    /// <summary>Signs out (audited).</summary>
    public async Task SignOutAsync(string badgeId, CancellationToken cancellationToken)
    {
        audit.Append(AuditEntry.Record(AuditEventType.SignOut, badgeId, clock.UtcNow, subject: options.StationId));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Verifies a supervisor badge + PIN and issues a single-use grant for one action.</summary>
    public async Task<ErrorOr<StepUpGrantResult>> RequestStepUpAsync(StepUpCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var validation = await stepUpValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToErrors();
        }

        var now = clock.UtcNow;
        var badge = StationUser.Normalise(command.SupervisorBadgeId);
        var supervisor = await users.FindByBadgeAsync(badge, cancellationToken);
        if (supervisor is null || supervisor.Role != StationRole.Supervisor)
        {
            audit.Append(AuditEntry.Record(AuditEventType.StepUpFailed, command.OperatorBadgeId, now, subject: badge, reason: supervisor is null ? "unknown badge" : "not a supervisor"));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return supervisor is null ? AccessErrors.StepUpFailed : AccessErrors.SupervisorRequired;
        }

        if (supervisor.IsLockedOut(now))
        {
            audit.Append(AuditEntry.Record(AuditEventType.StepUpFailed, command.OperatorBadgeId, now, subject: badge, reason: "locked out"));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AccessErrors.LockedOut;
        }

        if (!pinHasher.Verify(supervisor, command.Pin))
        {
            var lockedNow = supervisor.RegisterFailedPin(now, new LockoutPolicy(options.MaxFailedPinAttempts, options.LockoutDuration));
            audit.Append(AuditEntry.Record(
                lockedNow ? AuditEventType.StepUpLockedOut : AuditEventType.StepUpFailed, command.OperatorBadgeId, now, subject: badge, reason: "wrong PIN"));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return lockedNow ? AccessErrors.LockedOut : AccessErrors.StepUpFailed;
        }

        supervisor.RegisterSuccessfulPin();
        var grant = StepUpGrant.Issue(supervisor, command.Action, now, options.StepUpGrantLifetime);
        grants.Add(grant);
        audit.Append(AuditEntry.Record(AuditEventType.StepUpGranted, command.OperatorBadgeId, now, subject: supervisor.BadgeId, reason: command.Action));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new StepUpGrantResult(grant.Id, supervisor.BadgeId, supervisor.DisplayName, grant.Action, grant.ExpiresAt);
    }

    /// <summary>
    /// Consumes a grant for <paramref name="action"/>. The caller saves the unit of work together with the
    /// privileged change (for example the override record in bolt 009).
    /// </summary>
    public async Task<ErrorOr<StepUpGrant>> ConsumeGrantAsync(Guid grantId, string action, CancellationToken cancellationToken)
    {
        var grant = await grants.FindAsync(grantId, cancellationToken);
        if (grant is null)
        {
            return AccessErrors.GrantInvalid("unknown grant");
        }

        var consumed = grant.Consume(action, clock.UtcNow);
        return consumed.IsError ? consumed.Errors : grant;
    }
}
