using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WeighingMS.Infrastructure.Persistence;

/// <summary>
/// Used only by <c>dotnet ef</c> (migrations add / database update). It reads <c>ConnectionStrings__Db</c> from the
/// environment and falls back to the local development database from <c>.env.example</c>. The running Api uses
/// configuration (<c>ConnectionStrings:Db</c>) instead.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<WeighingDbContext>
{
    /// <inheritdoc />
    public WeighingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Db")
            ?? "Host=localhost;Port=5433;Database=weighingms;Username=weighingms;Password=change-me";
        var options = new DbContextOptionsBuilder<WeighingDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new WeighingDbContext(options);
    }
}
