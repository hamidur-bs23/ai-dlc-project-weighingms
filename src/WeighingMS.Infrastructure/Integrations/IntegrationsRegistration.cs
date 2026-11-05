using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using WeighingMS.Application.Batches;
using WeighingMS.Application.Devices;
using WeighingMS.Infrastructure.Integrations.Health;
using WeighingMS.Infrastructure.Integrations.OrgaTex;
using WeighingMS.Infrastructure.Integrations.Scales;
using WeighingMS.Infrastructure.Integrations.Scanner;

namespace WeighingMS.Infrastructure.Integrations;

/// <summary>
/// Registers the typed HttpClients to the simulator with resilience (tech-stack: timeout, retry for safe reads,
/// circuit breaker). Weight reads are never retried: the next poll is the retry. POSTs are never retried.
/// </summary>
internal static class IntegrationsRegistration
{
    public static IServiceCollection AddDeviceIntegrations(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(SimulatorIntegrationOptions.SectionName).Get<SimulatorIntegrationOptions>() ?? new SimulatorIntegrationOptions();
        services.AddSingleton(options);

        services.AddHttpClient<IScaleDevice, ScaleDeviceClient>(c => c.BaseAddress = options.BaseUrl).AddResilienceHandler("scales", b => Configure(b, options));
        services.AddHttpClient<IScanner, ScannerClient>(c => c.BaseAddress = options.BaseUrl).AddResilienceHandler("scanner", b => Configure(b, options));
        services.AddHttpClient<IOrgatexGateway, OrgatexGatewayClient>(c => c.BaseAddress = options.BaseUrl).AddResilienceHandler("orgatex", b => Configure(b, options));
        services.AddHttpClient<PrinterStatusProbe>(c => c.BaseAddress = options.BaseUrl).AddResilienceHandler("printer-status", b => Configure(b, options));
        services.AddHttpClient<OrgatexReachabilityProbe>(c => c.BaseAddress = options.BaseUrl).AddResilienceHandler("orgatex-reachability", b => Configure(b, options));
        services.AddTransient<IDeviceHealthProbe>(sp => sp.GetRequiredService<PrinterStatusProbe>());
        services.AddTransient<IDeviceHealthProbe>(sp => sp.GetRequiredService<OrgatexReachabilityProbe>());
        return services;
    }

    /// <summary>
    /// Resilience waits (attempt timeout, retry delay, circuit break) are wall-clock waits on I/O, so they always use the
    /// real clock (<see cref="TimeProvider.System"/>), independent of the application's injected business clock.
    /// </summary>
    private static void Configure(ResiliencePipelineBuilder<HttpResponseMessage> builder, SimulatorIntegrationOptions options)
    {
        builder.TimeProvider = TimeProvider.System;
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 1,
            Delay = TimeSpan.FromMilliseconds(100),
            UseJitter = true,
            ShouldHandle = args => ValueTask.FromResult(IsSafeToRetry(args.Context.GetRequestMessage(), args.Outcome)),
        });
        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            MinimumThroughput = Math.Max(2, options.CircuitBreakerMinimumThroughput),
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = options.CircuitBreakerBreakDuration,
        });
        builder.AddTimeout(options.AttemptTimeout);
    }

    /// <summary>Retry only GETs that are not weight polls, and only on transient failures.</summary>
    private static bool IsSafeToRetry(HttpRequestMessage? request, Outcome<HttpResponseMessage> outcome)
    {
        if (request is null || request.Method != HttpMethod.Get || request.RequestUri?.AbsolutePath.EndsWith("/weight", StringComparison.Ordinal) == true)
        {
            return false;
        }

        return outcome.Exception is HttpRequestException
            || outcome.Result?.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout;
    }
}
