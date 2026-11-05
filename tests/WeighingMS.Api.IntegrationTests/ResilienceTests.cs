using System.Net;
using System.Net.Sockets;
using WeighingMS.Application.Devices;
using WeighingMS.Infrastructure.Integrations.Scales;
using WeighingMS.Infrastructure.Integrations.Scanner;

namespace WeighingMS.Api.IntegrationTests;

/// <summary>Adapter failure translation (IntegrationCall) and the circuit breaker.</summary>
public sealed class ResilienceTests(ApiFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    [Fact]
    public async Task NothingListening_ConnectionRefused_Returns503AndHealthOffline()
    {
        await using var api = fixture.CreateApi(new Dictionary<string, string?> { ["Integrations:Simulators:BaseUrl"] = $"http://127.0.0.1:{FreePort()}" });
        using var client = new StationClient(api);
        (await client.SignInAsync()).EnsureSuccessStatusCode();

        var read = await client.GetAsync("/api/scales/scale-3kg/weight");
        var health = await (await client.GetAsync("/api/devices/health")).ReadJsonAsync();

        read.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await read.ErrorCodeAsync()).ShouldBe("Scale.Unavailable");
        (await client.GetAsync("/api/scales")).StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        health.GetProperty("items").EnumerateArray().ShouldAllBe(i => i.GetProperty("status").GetString() == "offline");
    }

    [Fact]
    public async Task RepeatedFailures_OpenTheCircuit_ThenCallsFailFastWith503()
    {
        await using var simulator = new SimulatorHost();
        await using var api = fixture.CreateApi(
            new Dictionary<string, string?>
            {
                ["Integrations:Simulators:CircuitBreakerMinimumThroughput"] = "2",
                ["Integrations:Simulators:CircuitBreakerBreakDuration"] = "00:00:30",
            },
            simulator);
        using var client = new StationClient(api);
        (await client.SignInAsync()).EnsureSuccessStatusCode();
        await simulator.FaultAsync("scale:scale-3kg", "error500");
        for (var i = 0; i < 3; i++)
        {
            (await client.GetAsync("/api/scales/scale-3kg/weight")).StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        }

        await simulator.ClearFaultsAsync();

        // The device is healthy again, but the circuit is open: the call fails fast without reaching it.
        var whileOpen = await client.GetAsync("/api/scales/scale-3kg/weight");
        whileOpen.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await whileOpen.ErrorCodeAsync()).ShouldBe("Scale.Unavailable");
    }

    [Fact]
    public async Task ScaleAdapter_MalformedBody_IsAMalformedReadingNotAnError()
    {
        var adapter = new ScaleDeviceClient(new HttpClient(new StubHandler(HttpStatusCode.OK, "{ not json")) { BaseAddress = new Uri("http://device") });

        var reading = await adapter.ReadAsync("scale-3kg", TestContext.Current.CancellationToken);

        reading.IsError.ShouldBeFalse();
        ScaleReadingRules.Classify(reading.Value, 0.5m).ShouldBe(ReadingValidity.Malformed);
    }

    [Fact]
    public async Task ScaleAdapter_ValueMissing_IsMalformed()
    {
        var adapter = new ScaleDeviceClient(new HttpClient(new StubHandler(HttpStatusCode.OK, """{"scaleId":"scale-3kg","unit":"g","stable":true,"overCapacity":false,"timestamp":"2026-11-03T21:48:22Z"}""")) { BaseAddress = new Uri("http://device") });

        var reading = await adapter.ReadAsync("scale-3kg", TestContext.Current.CancellationToken);

        reading.Value.Net.ShouldBeNull();
        ScaleReadingRules.Classify(reading.Value, 0.5m).ShouldBe(ReadingValidity.Malformed);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Scale.Unavailable")]
    [InlineData(HttpStatusCode.InternalServerError, "Scale.Unavailable")]
    [InlineData(HttpStatusCode.NotFound, "Scale.NotFound")]
    public async Task ScaleAdapter_HttpFailures_AreTranslated(HttpStatusCode status, string code)
    {
        var adapter = new ScaleDeviceClient(new HttpClient(new StubHandler(status, "")) { BaseAddress = new Uri("http://device") });

        (await adapter.TareAsync("scale-3kg", TestContext.Current.CancellationToken)).FirstError.Code.ShouldBe(code);
    }

    [Fact]
    public async Task ScannerAdapter_TransportException_IsScannerUnavailable()
    {
        var adapter = new ScannerClient(new HttpClient(new ThrowingHandler()) { BaseAddress = new Uri("http://device") });

        (await adapter.GetScansAfterAsync(0, TestContext.Current.CancellationToken)).FirstError.Code.ShouldBe("Scanner.Unavailable");
        (await adapter.GetStatusAsync(TestContext.Current.CancellationToken)).FirstError.Code.ShouldBe("Scanner.Unavailable");
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"), RequestMessage = request });
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("connection refused");
    }
}
