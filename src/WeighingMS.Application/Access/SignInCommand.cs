namespace WeighingMS.Application.Access;

/// <summary>Sign in with a badge. <paramref name="CurrentBadgeId"/> is the badge of the session being replaced, if any.</summary>
public sealed record SignInCommand(string BadgeId, string? Shift, string? CurrentBadgeId);
