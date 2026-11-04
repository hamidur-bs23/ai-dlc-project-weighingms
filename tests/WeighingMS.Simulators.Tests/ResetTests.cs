using System.Net;
using System.Net.Http.Json;

namespace WeighingMS.Simulators.Tests;

public sealed class ResetTests : IDisposable
{
    private readonly SimulatorFactory _factory = new();
    private readonly HttpClient _client;

    public ResetTests() => _client = _factory.CreateClient();

    [Fact]
    public async Task Reset_AfterActivity_ReturnsEveryDummyToItsStartState()
    {
        var ct = TestContext.Current.CancellationToken;
        await _client.PostJsonAsync("/api/scales/scale-3kg/tare", new { });
        await _client.PostJsonAsync("/_sim/scales/scale-3kg/scenario", new { scenario = "rising", target = new { value = 94.0m, unit = "g" } });
        _factory.Clock.Advance(TimeSpan.FromSeconds(8));
        await _client.SendAsync(
            new HttpRequestMessage(HttpMethod.Post, "/api/batches/B-24187/consumption")
            {
                Headers = { { "Idempotency-Key", "r-1" } },
                Content = JsonContent.Create(new { lines = new[] { new { chemicalId = "DYE-4471", actualQuantity = 94.0m, unit = "g", lot = "L1" } } }),
            },
            ct);
        await _client.SendAsync(
            new HttpRequestMessage(HttpMethod.Post, "/api/tickets")
            {
                Headers = { { "Idempotency-Key", "r-2" } },
                Content = JsonContent.Create(new { batchId = "B-24187", isReprint = false, lines = new[] { "x" } }),
            },
            ct);
        await _client.PostJsonAsync("/_sim/scanner/scan", new { code = "B-24187" });
        await _client.PostJsonAsync("/_sim/faults", new { target = "scanner", mode = "unavailable" });

        (await _client.PostJsonAsync("/_sim/reset", new { })).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await (await _client.GetUrlAsync("/api/scales/scale-3kg/weight")).ReadJsonAsync()).GetProperty("net").GetDecimal().ShouldBe(214.0m);
        (await (await _client.GetUrlAsync("/_sim/orgatex/consumption")).ReadJsonAsync()).GetArrayLength().ShouldBe(0);
        (await (await _client.GetUrlAsync("/_sim/printer/tickets")).ReadJsonAsync()).GetArrayLength().ShouldBe(0);
        (await (await _client.GetUrlAsync("/_sim/faults")).ReadJsonAsync()).GetArrayLength().ShouldBe(0);
        (await (await _client.GetUrlAsync("/api/scanner/scans")).ReadJsonAsync()).GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Reset_ScannerSequence_RestartsAtOne()
    {
        await _client.PostJsonAsync("/_sim/scanner/scan", new { code = "A" });
        await _client.PostJsonAsync("/_sim/reset", new { });

        var scan = await (await _client.PostJsonAsync("/_sim/scanner/scan", new { code = "B" })).ReadJsonAsync();

        scan.GetProperty("sequence").GetInt64().ShouldBe(1);
    }

    [Fact]
    public async Task Reset_KeepsTheSeedData()
    {
        await _client.PostJsonAsync("/_sim/reset", new { });

        (await (await _client.GetUrlAsync("/api/batches")).ReadJsonAsync()).GetArrayLength().ShouldBe(4);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
