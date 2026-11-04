using System.Net;

namespace WeighingMS.Simulators.Tests;

public sealed class ScannerTests : IDisposable
{
    private readonly SimulatorFactory _factory = new();
    private readonly HttpClient _client;

    public ScannerTests() => _client = _factory.CreateClient();

    private Task<HttpResponseMessage> ScanAsync(string code) => _client.PostJsonAsync("/_sim/scanner/scan", new { code });

    [Fact]
    public async Task Scan_Code_Returns202WithIncreasingSequence()
    {
        var first = await ScanAsync("B-24187");
        var second = await (await ScanAsync("2026-0387")).ReadJsonAsync();

        first.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        (await first.ReadJsonAsync()).GetProperty("sequence").GetInt64().ShouldBe(1);
        second.GetProperty("sequence").GetInt64().ShouldBe(2);
    }

    [Fact]
    public async Task Collect_FromZero_ReturnsAllScansInEmissionOrder()
    {
        await ScanAsync("B-24187");
        await ScanAsync("2026-0387");

        var json = await (await _client.GetUrlAsync("/api/scanner/scans?after=0")).ReadJsonAsync();

        json.EnumerateArray().Select(s => s.GetProperty("code").GetString()).ShouldBe(["B-24187", "2026-0387"]);
    }

    [Fact]
    public async Task Collect_WithCursor_ReturnsEachScanOnce()
    {
        await ScanAsync("B-24187");
        var firstPoll = await (await _client.GetUrlAsync("/api/scanner/scans?after=0")).ReadJsonAsync();
        var cursor = firstPoll.EnumerateArray().Last().GetProperty("sequence").GetInt64();

        var secondPoll = await (await _client.GetUrlAsync($"/api/scanner/scans?after={cursor}")).ReadJsonAsync();
        await ScanAsync("B-24188");
        var thirdPoll = await (await _client.GetUrlAsync($"/api/scanner/scans?after={cursor}")).ReadJsonAsync();

        secondPoll.GetArrayLength().ShouldBe(0);
        thirdPoll.EnumerateArray().Select(s => s.GetProperty("code").GetString()).ShouldBe(["B-24188"]);
    }

    [Fact]
    public async Task Collect_RepeatedPollWithSameCursor_IsSafeToRetry()
    {
        await ScanAsync("B-24187");

        var a = await (await _client.GetUrlAsync("/api/scanner/scans?after=0")).ReadJsonAsync();
        var b = await (await _client.GetUrlAsync("/api/scanner/scans?after=0")).ReadJsonAsync();

        a.GetArrayLength().ShouldBe(1);
        b.GetArrayLength().ShouldBe(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Scan_EmptyCode_Returns400(string code)
    {
        (await ScanAsync(code)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Collect_NegativeCursor_Returns400()
    {
        var response = await _client.GetUrlAsync("/api/scanner/scans?after=-1");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ProblemTypeAsync()).ShouldBe("Scanner.InvalidCursor");
    }

    [Fact]
    public async Task Status_NoFault_IsReady()
    {
        var json = await (await _client.GetUrlAsync("/api/scanner/status")).ReadJsonAsync();

        json.GetProperty("device").GetString().ShouldBe("scanner");
        json.GetProperty("status").GetString().ShouldBe("READY");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
