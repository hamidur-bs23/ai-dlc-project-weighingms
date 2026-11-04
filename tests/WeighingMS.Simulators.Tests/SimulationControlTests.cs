using System.Net;

namespace WeighingMS.Simulators.Tests;

public sealed class SimulationControlTests(ProductionSimulatorFactory factory) : IClassFixture<ProductionSimulatorFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Production_SimScenarioEndpoint_DoesNotExist()
    {
        var response = await _client.PostJsonAsync(
            "/_sim/scales/scale-3kg/scenario", new { scenario = "rising", target = new { value = 94.0m, unit = "g" } });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Production_SimConsumptionInspection_DoesNotExist()
    {
        (await _client.GetUrlAsync("/_sim/orgatex/consumption")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/_sim/faults")]
    [InlineData("/_sim/printer/tickets")]
    public async Task Production_SimGetEndpoints_DoNotExist(string url)
    {
        (await _client.GetUrlAsync(url)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/_sim/scanner/scan")]
    [InlineData("/_sim/reset")]
    [InlineData("/_sim/faults")]
    public async Task Production_SimPostEndpoints_DoNotExist(string url)
    {
        (await _client.PostJsonAsync(url, new { code = "B-24187", target = "orgatex", mode = "unavailable" })).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Production_TrainingContracts_StillServed()
    {
        (await _client.GetUrlAsync("/api/batches")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public void SimulatorHost_ReferencesNoWeighingMsBusinessAssembly()
    {
        typeof(Program).Assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ShouldNotContain(n => n != null && (n.StartsWith("WeighingMS.Domain", StringComparison.Ordinal)
                || n.StartsWith("WeighingMS.Application", StringComparison.Ordinal)
                || n.StartsWith("WeighingMS.Infrastructure", StringComparison.Ordinal)));
    }
}
