using WeighingMS.Domain.Access;

namespace WeighingMS.Application.Access;

/// <summary>Station users (operators and supervisors).</summary>
public interface IStationUserRepository
{
    /// <summary>Finds a user by badge (normalised), tracked for changes.</summary>
    Task<StationUser?> FindByBadgeAsync(string badgeId, CancellationToken cancellationToken);
}
