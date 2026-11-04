using Npgsql;
using WeighingMS.Infrastructure.Access;

namespace WeighingMS.Api.Common;

/// <summary>
/// Creates the lab users from <c>StationAccess:SeedUsers</c> at start-up (Development/Test only). If the database
/// is not migrated or not reachable, start-up continues with a warning that says what to run.
/// </summary>
public static partial class DevelopmentSeeding
{
    /// <summary>Seeds the configured users, if any.</summary>
    public static async Task SeedUsersAsync(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var seedUsers = app.Configuration.GetSection("StationAccess:SeedUsers").Get<List<SeedUserOptions>>() ?? [];
        if (seedUsers.Count == 0)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        try
        {
            await scope.ServiceProvider.GetRequiredService<DevelopmentUserSeeder>().SeedAsync(seedUsers, CancellationToken.None);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            LogNotMigrated(app.Logger, ex);
        }
        catch (NpgsqlException ex)
        {
            LogUnreachable(app.Logger, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Database is not migrated; skipped seeding users. Run `npm run db:update`.")]
    private static partial void LogNotMigrated(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Database is not reachable; skipped seeding users. Start it with `docker compose up -d postgres`.")]
    private static partial void LogUnreachable(ILogger logger, Exception exception);
}
