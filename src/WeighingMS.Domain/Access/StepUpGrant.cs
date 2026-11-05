using ErrorOr;

namespace WeighingMS.Domain.Access;

/// <summary>
/// A single-use supervisor authorisation for one privileged action (ADR-009). It is issued after a correct
/// badge + PIN, expires after a short time, and can be consumed once, for its own action only.
/// </summary>
public sealed class StepUpGrant
{
    private StepUpGrant()
    {
        Action = string.Empty;
        SupervisorBadgeId = string.Empty;
        SupervisorName = string.Empty;
    }

    /// <summary>Identifier (UUIDv7); returned to the client as the grant id.</summary>
    public Guid Id { get; private set; }

    /// <summary>The supervisor who authorised.</summary>
    public Guid SupervisorId { get; private set; }

    /// <summary>Supervisor badge (for records and audit).</summary>
    public string SupervisorBadgeId { get; private set; }

    /// <summary>Supervisor name (for records and tickets).</summary>
    public string SupervisorName { get; private set; }

    /// <summary>The action authorised, for example <c>override</c>.</summary>
    public string Action { get; private set; }

    /// <summary>When the grant was issued (UTC).</summary>
    public DateTimeOffset IssuedAt { get; private set; }

    /// <summary>When the grant stops being valid (UTC).</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>When the grant was used (UTC), if it was.</summary>
    public DateTimeOffset? UsedAt { get; private set; }

    /// <summary>Issues a grant for a supervisor.</summary>
    public static StepUpGrant Issue(StationUser supervisor, string action, DateTimeOffset now, TimeSpan lifetime)
    {
        ArgumentNullException.ThrowIfNull(supervisor);
        if (supervisor.Role != StationRole.Supervisor)
        {
            throw new InvalidOperationException("Only a supervisor can be issued a step-up grant.");
        }

        return new()
        {
            Id = Guid.CreateVersion7(),
            SupervisorId = supervisor.Id,
            SupervisorBadgeId = supervisor.BadgeId,
            SupervisorName = supervisor.DisplayName,
            Action = action,
            IssuedAt = now,
            ExpiresAt = now + lifetime,
        };
    }

    /// <summary>Uses the grant for <paramref name="action"/>; fails if used, expired or for another action.</summary>
    public ErrorOr<Success> Consume(string action, DateTimeOffset now)
    {
        var usable = CheckUsable(action, now);
        if (usable.IsError)
        {
            return usable.Errors;
        }

        UsedAt = now;
        return Result.Success;
    }

    /// <summary>
    /// Whether the grant could be consumed now for <paramref name="action"/>, without consuming it: an override checks
    /// the grant first and consumes it only when the record is written, so a fixable rejection leaves it unused.
    /// </summary>
    public ErrorOr<Success> CheckUsable(string action, DateTimeOffset now)
    {
        if (UsedAt is not null)
        {
            return AccessErrors.GrantInvalid("already used");
        }

        if (now >= ExpiresAt)
        {
            return AccessErrors.GrantInvalid("expired");
        }

        if (!string.Equals(action, Action, StringComparison.Ordinal))
        {
            return AccessErrors.GrantInvalid($"issued for '{Action}', not '{action}'");
        }

        return Result.Success;
    }
}
