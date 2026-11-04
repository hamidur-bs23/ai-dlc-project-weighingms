namespace WeighingMS.Domain.Access;

/// <summary>How many wrong PINs lock a supervisor, and for how long.</summary>
public sealed record LockoutPolicy(int MaxFailedAttempts, TimeSpan LockoutDuration);
