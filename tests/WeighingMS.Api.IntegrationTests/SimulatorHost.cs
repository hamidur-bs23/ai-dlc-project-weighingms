extern alias sim;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using SimProgram = sim::Program;

namespace WeighingMS.Api.IntegrationTests;

/// <summary>
/// The real WeighingMS.Simulators.Host, in-process (environment <c>Test</c>, so <c>/_sim/*</c> is available), with its
/// own controllable clock for scale scenarios. The Api's typed HttpClients are routed to it.
/// </summary>
public sealed class SimulatorHost : WebApplicationFactory<SimProgram>
{
    /// <summary>The simulator's clock (scenario progress).</summary>
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 3, 21, 48, 22, TimeSpan.Zero));

    /// <summary>
    /// Starts the simulator on a real loopback port (Kestrel) instead of the in-memory test server, so outgoing calls
    /// go through SocketsHttpHandler like in production (needed to observe W3C trace propagation). Returns its URL.
    /// </summary>
    public Uri StartOnLoopback()
    {
        UseKestrel(0);
        StartServer();
        var addresses = Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
            .Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()!.Addresses;
        return new Uri(addresses.First());
    }

    /// <summary>A client for the simulator's training contracts and <c>/_sim/*</c> control.</summary>
    public HttpClient Control => _control ??= CreateClient();

    private HttpClient? _control;

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    /// <summary>Posts a JSON body to the simulator.</summary>
    public async Task PostAsync(string url, object body) =>
        (await Control.PostAsync(new Uri(url, UriKind.Relative), System.Net.Http.Json.JsonContent.Create(body), TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

    /// <summary>Injects a fault.</summary>
    public Task FaultAsync(string target, string mode) => PostAsync("/_sim/faults", new { target, mode });

    /// <summary>Clears all faults.</summary>
    public async Task ClearFaultsAsync() =>
        (await Control.DeleteAsync(new Uri("/_sim/faults", UriKind.Relative), TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
}
