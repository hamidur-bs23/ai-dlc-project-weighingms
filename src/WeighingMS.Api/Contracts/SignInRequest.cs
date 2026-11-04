namespace WeighingMS.Api.Contracts;

/// <summary>Sign in with a typed badge number and an optional shift.</summary>
public sealed record SignInRequest(string BadgeId, string? Shift);
