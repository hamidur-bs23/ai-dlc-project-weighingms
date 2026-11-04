namespace WeighingMS.Domain.Audit;

/// <summary>
/// One immutable audit record: who did what, to what, why and when (UTC). There is no way to change an entry
/// after it is created; the trail is append-only.
/// </summary>
public sealed class AuditEntry
{
    private AuditEntry()
    {
        ActorBadgeId = string.Empty;
    }

    /// <summary>Identifier (UUIDv7, time-ordered).</summary>
    public Guid Id { get; private set; }

    /// <summary>The event.</summary>
    public AuditEventType Type { get; private set; }

    /// <summary>Badge of the person who acted.</summary>
    public string ActorBadgeId { get; private set; }

    /// <summary>What the event is about (for example a batch or a supervisor badge), if anything.</summary>
    public string? Subject { get; private set; }

    /// <summary>Reason given, if any (mandatory for overrides).</summary>
    public string? Reason { get; private set; }

    /// <summary>When it happened (UTC).</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Creates an entry.</summary>
    public static AuditEntry Record(AuditEventType type, string actorBadgeId, DateTimeOffset occurredAt, string? subject = null, string? reason = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorBadgeId);
        return new()
        {
            Id = Guid.CreateVersion7(),
            Type = type,
            ActorBadgeId = actorBadgeId,
            Subject = subject,
            Reason = reason,
            OccurredAt = occurredAt.ToUniversalTime(),
        };
    }
}
