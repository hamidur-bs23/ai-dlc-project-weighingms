using WeighingMS.Domain.Access;

namespace WeighingMS.Application.Access;

/// <summary>The signed-in operator session.</summary>
public sealed record SessionInfo(
    Guid UserId,
    string BadgeId,
    string DisplayName,
    StationRole Role,
    string? Shift,
    string StationId,
    string TimeZone,
    DateTimeOffset SignedInAt);
