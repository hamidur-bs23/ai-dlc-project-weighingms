using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace WeighingMS.Simulators.Tests;

/// <summary>
/// Hosts the simulator in-process. The clock is a <see cref="FakeTimeProvider"/>, so scale scenarios can be stepped
/// exactly. The environment defaults to <c>Test</c>, which enables <c>/_sim/*</c>.
/// </summary>
public class SimulatorFactory : WebApplicationFactory<Program>
{
    /// <summary>The controllable clock used by the simulator.</summary>
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 3, 21, 48, 0, TimeSpan.Zero));

    /// <summary>Hosting environment name.</summary>
    protected virtual string EnvironmentName => "Test";

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }
}

/// <summary>The simulator in the Production environment, where <c>/_sim/*</c> must not exist.</summary>
public sealed class ProductionSimulatorFactory : SimulatorFactory
{
    /// <inheritdoc />
    protected override string EnvironmentName => "Production";
}
