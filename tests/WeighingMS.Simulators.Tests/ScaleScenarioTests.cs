using System.Globalization;
using System.Net;

namespace WeighingMS.Simulators.Tests;

public sealed class ScaleScenarioTests : IDisposable
{
    private static readonly TimeSpan _pourDuration = TimeSpan.FromSeconds(8);
    private readonly SimulatorFactory _factory = new();
    private readonly HttpClient _client;

    public ScaleScenarioTests() => _client = _factory.CreateClient();

    private async Task TareAndStartAsync(string scaleId, string scenario, decimal target, string unit, decimal? tolerance)
    {
        (await _client.PostJsonAsync($"/api/scales/{scaleId}/tare", new { })).EnsureSuccessStatusCode();
        var response = await _client.PostJsonAsync(
            $"/_sim/scales/{scaleId}/scenario", new { scenario, target = new { value = target, unit }, tolerance });
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private async Task<(decimal Net, bool Stable, bool OverCapacity)> ReadAsync(string scaleId)
    {
        var json = await (await _client.GetUrlAsync($"/api/scales/{scaleId}/weight")).ReadJsonAsync();
        return (json.GetProperty("net").GetDecimal(), json.GetProperty("stable").GetBoolean(), json.GetProperty("overCapacity").GetBoolean());
    }

    [Theory]
    [InlineData("rising", "94.0")]
    [InlineData("belowRange", "93.0")]
    [InlineData("enterRange", "94.2")]
    [InlineData("overshoot", "96.4")]
    public async Task Scenario_YellowRgbOnPrecisionScale_LandsOnExpectedNetAndIsStable(string scenario, string expected)
    {
        await TareAndStartAsync("scale-3kg", scenario, 94.0m, "g", 0.5m);
        _factory.Clock.Advance(_pourDuration);

        var (net, stable, _) = await ReadAsync("scale-3kg");

        net.ShouldBe(decimal.Parse(expected, CultureInfo.InvariantCulture));
        stable.ShouldBeTrue();
    }

    [Theory]
    [InlineData("rising", "24.000")]
    [InlineData("belowRange", "23.760")]
    [InlineData("enterRange", "24.048")]
    [InlineData("overshoot", "24.576")]
    public async Task Scenario_SodiumSulphateOnFloorScale_LandsOnExpectedNet(string scenario, string expected)
    {
        await TareAndStartAsync("scale-32kg", scenario, 24.000m, "kg", 0.120m);
        _factory.Clock.Advance(_pourDuration);

        (await ReadAsync("scale-32kg")).Net.ShouldBe(decimal.Parse(expected, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Scenario_MidPour_IsRisingUnstableAndOnTheResolutionGrid()
    {
        await TareAndStartAsync("scale-3kg", "rising", 94.0m, "g", null);
        var seen = new List<decimal>();
        for (var i = 1; i < 8; i++)
        {
            _factory.Clock.Advance(TimeSpan.FromSeconds(1));
            var (net, stable, _) = await ReadAsync("scale-3kg");
            stable.ShouldBeFalse();
            (net % 0.1m).ShouldBe(0m);
            seen.Add(net);
        }

        seen.ShouldBeInOrder(SortDirection.Ascending);
        seen[^1].ShouldBeLessThan(94.0m);
    }

    [Fact]
    public async Task Scenario_Stable_HoldsTheCurrentReading()
    {
        await TareAndStartAsync("scale-3kg", "rising", 94.0m, "g", null);
        _factory.Clock.Advance(TimeSpan.FromSeconds(2));
        var before = (await ReadAsync("scale-3kg")).Net;
        (await _client.PostJsonAsync("/_sim/scales/scale-3kg/scenario", new { scenario = "stable" })).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        _factory.Clock.Advance(TimeSpan.FromSeconds(30));
        var (after, stable, _) = await ReadAsync("scale-3kg");

        after.ShouldBe(before);
        stable.ShouldBeTrue();
    }

    [Fact]
    public async Task Scenario_KilogramTargetOnGramScale_IsConverted()
    {
        await TareAndStartAsync("scale-3kg", "rising", 0.930m, "kg", null);
        _factory.Clock.Advance(_pourDuration);

        (await ReadAsync("scale-3kg")).Net.ShouldBe(930.0m);
    }

    [Fact]
    public async Task Scenario_OvershootWithoutTolerance_Returns400ScenarioInvalid()
    {
        var response = await _client.PostJsonAsync(
            "/_sim/scales/scale-3kg/scenario", new { scenario = "overshoot", target = new { value = 94.0m, unit = "g" } });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ProblemTypeAsync()).ShouldBe("Scenario.Invalid");
    }

    [Fact]
    public async Task Scenario_AboveCapacity_IsFlaggedAndCapped()
    {
        await TareAndStartAsync("scale-3kg", "rising", 5.000m, "kg", null);
        _factory.Clock.Advance(_pourDuration);

        var (net, _, overCapacity) = await ReadAsync("scale-3kg");

        overCapacity.ShouldBeTrue();
        net.ShouldBe(3200.0m);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task EmptyContainer_AfterAPour_ReadsMinusTheOldPourUntilTaredThenPoursFromZero()
    {
        await TareAndStartAsync("scale-3kg", "rising", 1240.0m, "g", null);
        _factory.Clock.Advance(_pourDuration);
        (await ReadAsync("scale-3kg")).Net.ShouldBe(1240.0m);
        (await _client.PostJsonAsync("/api/scales/scale-3kg/tare", new { })).EnsureSuccessStatusCode();

        (await _client.PostJsonAsync("/_sim/scales/scale-3kg/container", new { })).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ReadAsync("scale-3kg")).Net.ShouldBe(-1240.0m);

        await TareAndStartAsync("scale-3kg", "rising", 386.0m, "g", null);
        (await ReadAsync("scale-3kg")).Net.ShouldBe(0.0m);
        _factory.Clock.Advance(_pourDuration);
        var (net, stable, _) = await ReadAsync("scale-3kg");

        net.ShouldBe(386.0m);
        stable.ShouldBeTrue();
    }

    [Fact]
    public async Task EmptyContainer_UnknownScale_Returns404()
    {
        (await _client.PostJsonAsync("/_sim/scales/scale-9kg/container", new { })).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
