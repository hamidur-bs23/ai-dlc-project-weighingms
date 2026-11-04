using Microsoft.EntityFrameworkCore;
using WeighingMS.Application.Access;
using WeighingMS.Domain.Access;
using WeighingMS.Infrastructure.Persistence;

namespace WeighingMS.Infrastructure.Access;

/// <summary>EF Core implementation of <see cref="IStationUserRepository"/>.</summary>
public sealed class StationUserRepository(WeighingDbContext db) : IStationUserRepository
{
    /// <inheritdoc />
    public Task<StationUser?> FindByBadgeAsync(string badgeId, CancellationToken cancellationToken) =>
        db.StationUsers.FirstOrDefaultAsync(u => u.BadgeId == badgeId, cancellationToken);
}
