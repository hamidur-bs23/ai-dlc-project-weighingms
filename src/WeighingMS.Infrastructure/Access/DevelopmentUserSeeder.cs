using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WeighingMS.Application.Access;
using WeighingMS.Domain.Access;
using WeighingMS.Infrastructure.Persistence;

namespace WeighingMS.Infrastructure.Access;

/// <summary>Idempotently creates the lab users (missing badges only). Never used in Production.</summary>
public sealed partial class DevelopmentUserSeeder(WeighingDbContext db, IPinHasher pinHasher, ILogger<DevelopmentUserSeeder> logger)
{
    /// <summary>Creates any seed users that do not exist yet.</summary>
    public async Task SeedAsync(IEnumerable<SeedUserOptions> seedUsers, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(seedUsers);
        var created = 0;
        foreach (var seed in seedUsers)
        {
            var badge = StationUser.Normalise(seed.BadgeId);
            if (await db.StationUsers.AnyAsync(u => u.BadgeId == badge, cancellationToken))
            {
                continue;
            }

            var user = string.Equals(seed.Role, nameof(StationRole.Supervisor), StringComparison.OrdinalIgnoreCase)
                ? StationUser.CreateSupervisor(badge, seed.DisplayName, pinHasher.Hash(seed.Pin ?? throw new InvalidOperationException($"Seed supervisor '{badge}' needs a PIN.")))
                : StationUser.CreateOperator(badge, seed.DisplayName);
            db.StationUsers.Add(user);
            created++;
        }

        await db.SaveChangesAsync(cancellationToken);
        LogSeeded(logger, created);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Development seeder created {Count} station users")]
    private static partial void LogSeeded(ILogger logger, int count);
}
