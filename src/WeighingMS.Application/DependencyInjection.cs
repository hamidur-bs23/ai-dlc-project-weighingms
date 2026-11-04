using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WeighingMS.Application.Access;
using WeighingMS.Application.Devices;

namespace WeighingMS.Application;

/// <summary>Registers the Application layer.</summary>
public static class DependencyInjection
{
    /// <summary>Adds use cases and validators.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<IApplicationAssemblyMarker>(ServiceLifetime.Singleton);
        services.AddScoped<StationAccessService>();
        services.AddSingleton<DeviceHealthTracker>();
        services.AddScoped<DeviceService>();
        return services;
    }
}
