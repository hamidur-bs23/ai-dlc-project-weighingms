using System.Net;

namespace WeighingMS.Simulators.Tests;

public sealed class ScaleTests : IDisposable
{
    private readonly SimulatorFactory _factory = new();
    private readonly HttpClient _client;

    public ScaleTests() => _client = _factory.CreateClient();

    [Fact]
    public async Task GetScales_Seed_ListsPrecisionAndHeavyDutyWithResolution()
    {
        var json = await (await _client.GetUrlAsync("/api/scales")).ReadJsonAsync();
        var scales = json.EnumerateArray().ToDictionary(s => s.GetProperty("scaleId").GetString()!);

        scales["scale-3kg"].GetProperty("scaleClass").GetString().ShouldBe("precision");
        scales["scale-3kg"].GetProperty("unit").GetString().ShouldBe("g");
        scales["scale-3kg"].GetProperty("resolution").GetDecimal().ShouldBe(0.1m);
        scales["scale-32kg"].GetProperty("scaleClass").GetString().ShouldBe("heavyDuty");
        scales["scale-32kg"].GetProperty("capacity").GetDecimal().ShouldBe(32.000m);
        scales["scale-32kg"].GetProperty("resolution").GetDecimal().ShouldBe(0.001m);
    }

    [Theory]
    [InlineData("scale-3kg")]
    [InlineData("scale-32kg")]
    public async Task GetStatus_KnownScale_IsReady(string scaleId)
    {
        var json = await (await _client.GetUrlAsync($"/api/scales/{scaleId}/status")).ReadJsonAsync();

        json.GetProperty("status").GetString().ShouldBe("READY");
    }

    [Theory]
    [InlineData("scale-3kg", "214.0")]
    [InlineData("scale-32kg", "1.850")]
    public async Task GetWeight_BeforeTare_ShowsTheEmptyContainer(string scaleId, string expected)
    {
        var json = await (await _client.GetUrlAsync($"/api/scales/{scaleId}/weight")).ReadJsonAsync();

        json.GetProperty("net").GetDecimal().ShouldBe(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
        json.GetProperty("stable").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Tare_EmptyContainer_ReportsTareAndNetBecomesZero()
    {
        var tare = await (await _client.PostJsonAsync("/api/scales/scale-3kg/tare", new { })).ReadJsonAsync();
        var reading = await (await _client.GetUrlAsync("/api/scales/scale-3kg/weight")).ReadJsonAsync();

        tare.GetProperty("tare").GetDecimal().ShouldBe(214.0m);
        tare.GetProperty("unit").GetString().ShouldBe("g");
        reading.GetProperty("net").GetDecimal().ShouldBe(0m);
    }

    [Theory]
    [InlineData("/api/scales/nope/status")]
    [InlineData("/api/scales/nope/weight")]
    public async Task Get_UnknownScale_Returns404ScaleNotFound(string url)
    {
        var response = await _client.GetUrlAsync(url);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.ProblemTypeAsync()).ShouldBe("Scale.NotFound");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
