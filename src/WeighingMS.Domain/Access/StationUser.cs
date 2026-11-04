namespace WeighingMS.Domain.Access;

/// <summary>
/// A person who can use the station. Operators are identified by badge only; supervisors also have a PIN
/// (stored hashed) for step-up authorisation, and are locked out after too many wrong PINs.
/// </summary>
public sealed class StationUser
{
    private StationUser()
    {
        BadgeId = string.Empty;
        DisplayName = string.Empty;
    }

    /// <summary>Identifier (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>Badge number, as typed at sign-in (unique).</summary>
    public string BadgeId { get; private set; }

    /// <summary>Name shown on the station and printed on tickets.</summary>
    public string DisplayName { get; private set; }

    /// <summary>Role.</summary>
    public StationRole Role { get; private set; }

    /// <summary>Hash of the supervisor PIN; null for operators.</summary>
    public string? PinHash { get; private set; }

    /// <summary>Consecutive wrong PINs since the last success or lockout.</summary>
    public int FailedPinAttempts { get; private set; }

    /// <summary>End of the current lockout (UTC), if any.</summary>
    public DateTimeOffset? LockedUntil { get; private set; }

    /// <summary>Creates an operator.</summary>
    public static StationUser CreateOperator(string badgeId, string displayName) =>
        new() { Id = Guid.CreateVersion7(), BadgeId = Normalise(badgeId), DisplayName = displayName.Trim(), Role = StationRole.Operator };

    /// <summary>Creates a supervisor with an already hashed PIN.</summary>
    public static StationUser CreateSupervisor(string badgeId, string displayName, string pinHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pinHash);
        return new()
        {
            Id = Guid.CreateVersion7(),
            BadgeId = Normalise(badgeId),
            DisplayName = displayName.Trim(),
            Role = StationRole.Supervisor,
            PinHash = pinHash,
        };
    }

    /// <summary>Badge numbers are compared trimmed and upper-case.</summary>
    public static string Normalise(string badgeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(badgeId);
        return badgeId.Trim().ToUpperInvariant();
    }

    /// <summary>True while a lockout is in force.</summary>
    public bool IsLockedOut(DateTimeOffset now) => LockedUntil is { } until && now < until;

    /// <summary>Records a wrong PIN; locks the supervisor when the policy limit is reached.</summary>
    /// <returns>True when this failure caused a lockout.</returns>
    public bool RegisterFailedPin(DateTimeOffset now, LockoutPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        FailedPinAttempts++;
        if (FailedPinAttempts < policy.MaxFailedAttempts)
        {
            return false;
        }

        LockedUntil = now + policy.LockoutDuration;
        FailedPinAttempts = 0;
        return true;
    }

    /// <summary>Clears failed attempts after a correct PIN.</summary>
    public void RegisterSuccessfulPin()
    {
        FailedPinAttempts = 0;
        LockedUntil = null;
    }
}
