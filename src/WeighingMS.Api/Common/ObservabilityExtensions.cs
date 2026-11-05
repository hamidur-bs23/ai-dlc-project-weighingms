using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WeighingMS.Application.Abstractions;

namespace WeighingMS.Api.Common;

/// <summary>
/// OpenTelemetry for WeighingMS.Api (tech-stack → Observability; decision OTEL-TIMING). Traces cover ASP.NET Core,
/// HttpClient (W3C trace context flows to the simulator) and Npgsql; healthy weight polls are dropped after
/// they finish (<see cref="HealthyWeightPollFilter"/>), failed ones keep their trace. Export
/// is enabled only when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>Service name of the Api.</summary>
    public const string ServiceName = "weighingms-api";

    /// <summary>Adds tracing and metrics.</summary>
    public static IServiceCollection AddWeighingTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var builder = services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(ServiceName))
            .WithTracing(t => t
                .AddSource(WmsTelemetry.Name)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddNpgsql()
                .AddProcessor(new HealthyWeightPollFilter()))
            .WithMetrics(m => m
                .AddMeter(WmsTelemetry.Name)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation());

        if (!string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.UseOtlpExporter();
        }

        return services;
    }

    /// <summary>A GET of a scale's live weight (polled every ~250 ms; measured by a metric instead of traced).</summary>
    public static bool IsWeightPoll(string method, string? path) =>
        HttpMethods.IsGet(method)
        && path is not null
        && ((path.StartsWith("/api/scales/", StringComparison.Ordinal) && path.EndsWith("/weight", StringComparison.Ordinal))
            || (path.StartsWith("/api/batches/", StringComparison.Ordinal) && path.EndsWith("/session/reading", StringComparison.Ordinal)));
}
