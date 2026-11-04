namespace WeighingMS.Api.Contracts;

/// <summary>The signed-in station session.</summary>
public sealed record SessionResponse(
    string BadgeId,
    string DisplayName,
    string Role,
    string? Shift,
    string StationId,
    string TimeZone,
    DateTimeOffset SignedInAt);
