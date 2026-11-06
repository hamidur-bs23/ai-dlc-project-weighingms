using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeighingMS.Application.Abstractions;
using WeighingMS.Application.Access;
using WeighingMS.Application.Batches;
using WeighingMS.Application.Tickets;
using WeighingMS.Application.Weighing;
using WeighingMS.Application.WriteBack;
using WeighingMS.Infrastructure.Access;
using WeighingMS.Infrastructure.Batches;
using WeighingMS.Infrastructure.Integrations;
using WeighingMS.Infrastructure.Persistence;
using WeighingMS.Infrastructure.Tickets;
using WeighingMS.Infrastructure.Weighing;
using WeighingMS.Infrastructure.WriteBack;

namespace WeighingMS.Infrastructure;

/// <summary>Registers the Infrastructure layer.</summary>
public static class DependencyInjection
{
    /// <summary>Adds persistence and the access adapters. Requires <c>ConnectionStrings:Db</c>.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Db")
            ?? throw new InvalidOperationException("ConnectionStrings:Db is not configured.");
        services.AddDbContext<WeighingDbContext>(o => o.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<WeighingDbContext>());
        services.AddScoped<IStationUserRepository, StationUserRepository>();
        services.AddScoped<IStepUpGrantStore, StepUpGrantStore>();
        services.AddScoped<IAuditTrail, AuditTrail>();
        services.AddSingleton<IPinHasher, PinHasher>();
        services.AddSingleton<IPlantClock, PlantClock>();
        services.AddScoped<DevelopmentUserSeeder>();
        services.AddScoped<IRecipeSnapshotStore, RecipeSnapshotStore>();
        services.AddSingleton<IToleranceCatalogue, ToleranceCatalogue>();
        services.AddScoped<IWeighingSessionRepository, WeighingSessionRepository>();
        services.AddSingleton<IScaleSelectionSource, ScaleSelectionSource>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IWriteBackRepository, WriteBackRepository>();
        services.AddSingleton<WriteBackSignal>();
        services.AddSingleton<IWriteBackSignal>(sp => sp.GetRequiredService<WriteBackSignal>());
        services.AddHostedService<WriteBackHostedService>();
        services.AddDeviceIntegrations(configuration);
        return services;
    }
}
