using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using OpenTelemetry.Trace;

namespace WeighingMS.Api.IntegrationTests;

/// <summary>OpenTelemetry (OTEL-TIMING): healthy weight polls are not traced; failed reads are; context flows outward.</summary>
public sealed class TracingTests(ApiFixture fixture) : IAsyncLifetime
{
    private readonly SimulatorHost _simulator = new();
    private readonly List<Activity> _spans = [];
    private WebApplicationFactory<Program> _api = null!;
    private StationClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        // Real sockets to the simulator: SocketsHttpHandler creates the client span and injects traceparent.
        var simulatorUrl = _simulator.StartOnLoopback();
        _api = fixture.CreateApi(new Dictionary<string, string?> { ["Integrations:Simulators:BaseUrl"] = simulatorUrl.ToString() }).WithWebHostBuilder(b =>
            b.ConfigureTestServices(s => s.ConfigureOpenTelemetryTracerProvider(t => t.AddInMemoryExporter(_spans))));
        _client = new StationClient(_api);
        (await _client.SignInAsync()).EnsureSuccessStatusCode();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _api.DisposeAsync();
        await _simulator.DisposeAsync();
    }

    private async Task<List<Activity>> SpansAfterAsync(Func<Task> act)
    {
        lock (_spans)
        {
            _spans.Clear();
        }

        await act();
        await Task.Delay(200, TestContext.Current.CancellationToken);
        lock (_spans)
        {
            return [.. _spans];
        }
    }

    [Fact]
    public async Task HealthyWeightPoll_ProducesNoSpans()
    {
        var spans = await SpansAfterAsync(async () => (await _client.GetAsync("/api/scales/scale-3kg/weight")).EnsureSuccessStatusCode());

        spans.Where(IsWeightSpan).ShouldBeEmpty();
    }

    [Fact]
    public async Task FailedWeightRead_ProducesAnErrorSpan()
    {
        await _simulator.FaultAsync("scale:scale-3kg", "unavailable");

        var spans = await SpansAfterAsync(() => _client.GetAsync("/api/scales/scale-3kg/weight"));

        var read = spans.Where(s => s.OperationName == "wms.scale.read").ShouldHaveSingleItem();
        read.Status.ShouldBe(ActivityStatusCode.Error);
        read.GetTagItem("wms.scale.id").ShouldBe("scale-3kg");
    }

    private static bool IsWeightSpan(Activity span)
    {
        var path = span.GetTagItem("url.path") as string ?? span.GetTagItem("url.full") as string ?? string.Empty;
        return path.EndsWith("/weight", StringComparison.Ordinal);
    }

    [Fact]
    public async Task StatusCall_IsOneTraceFromTheApiRequestToTheOutgoingDeviceCall()
    {
        var spans = await SpansAfterAsync(async () => (await _client.GetAsync("/api/scales/scale-3kg/status")).EnsureSuccessStatusCode());

        // The in-process simulator's request span is captured too: Api server span → outgoing client span →
        // simulator server span, all in one trace (W3C traceparent propagated).
        var servers = spans.Where(s => s.Kind == ActivityKind.Server && (s.GetTagItem("url.path") as string) == "/api/scales/scale-3kg/status").ToList();
        var outgoing = spans.Single(s => s.Kind == ActivityKind.Client && (s.GetTagItem("url.full") as string)?.EndsWith("/api/scales/scale-3kg/status", StringComparison.Ordinal) == true);
        var apiServer = servers.Single(s => s.SpanId == outgoing.ParentSpanId);
        var simulatorServer = servers.Single(s => s.ParentSpanId == outgoing.SpanId);
        outgoing.TraceId.ShouldBe(apiServer.TraceId);
        simulatorServer.TraceId.ShouldBe(apiServer.TraceId);
    }
}
