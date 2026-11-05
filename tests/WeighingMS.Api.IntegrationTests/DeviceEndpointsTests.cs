using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WeighingMS.Api.IntegrationTests;

/// <summary>Device endpoints through the Api, against the real simulator in-process (contracts and fault injection).</summary>
public sealed class DeviceEndpointsTests(ApiFixture fixture) : IAsyncLifetime
{
    private static readonly TimeSpan _pour = TimeSpan.FromSeconds(8);
    private readonly SimulatorHost _simulator = new();
    private WebApplicationFactory<Program> _api = null!;
    private StationClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        _api = fixture.CreateApi(simulator: _simulator);
        _client = new StationClient(_api);
        (await _client.SignInAsync()).EnsureSuccessStatusCode();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _api.DisposeAsync();
        await _simulator.DisposeAsync();
    }

    private async Task<JsonElement> GetJsonAsync(string url)
    {
        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.ReadJsonAsync();
    }

    private Task<HttpResponseMessage> TareAsync(string scaleId) => _client.SendAsync(HttpMethod.Post, $"/api/scales/{scaleId}/tare");

    [Fact]
    public async Task Scales_ListsBothScalesWithQuantitiesAndPollInterval()
    {
        var json = await GetJsonAsync("/api/scales");
        var precision = json.GetProperty("items")[0];

        json.GetProperty("pollIntervalMs").GetInt32().ShouldBe(250);
        json.GetProperty("items").GetArrayLength().ShouldBe(2);
        precision.GetProperty("scaleId").GetString().ShouldBe("scale-3kg");
        precision.GetProperty("capacity").GetProperty("value").GetDecimal().ShouldBe(3200.0m);
        precision.GetProperty("resolution").GetProperty("value").GetDecimal().ShouldBe(0.1m);
        precision.GetProperty("resolution").GetProperty("unit").GetString().ShouldBe("g");
        precision.GetProperty("status").GetString().ShouldBe("ready");
        json.GetProperty("items")[1].GetProperty("scaleClass").GetString().ShouldBe("heavyDuty");
    }

    [Fact]
    public async Task TareThenEnterRange_YellowRgb_ReadsStableValid94Point2Grams()
    {
        var tare = await (await TareAsync("scale-3kg")).ReadJsonAsync();
        tare.GetProperty("tare").GetProperty("value").GetDecimal().ShouldBe(214.0m);
        (await GetJsonAsync("/api/scales/scale-3kg/weight")).GetProperty("net").GetProperty("value").GetDecimal().ShouldBe(0.0m);

        await _simulator.PostAsync("/_sim/scales/scale-3kg/scenario", new { scenario = "enterRange", target = new { value = 94.0m, unit = "g" }, tolerance = 0.5m });
        _simulator.Clock.Advance(TimeSpan.FromSeconds(2));
        var mid = await GetJsonAsync("/api/scales/scale-3kg/weight");
        _simulator.Clock.Advance(_pour);
        var settled = await GetJsonAsync("/api/scales/scale-3kg/weight");

        mid.GetProperty("stable").GetBoolean().ShouldBeFalse();
        settled.GetProperty("net").GetProperty("value").GetDecimal().ShouldBe(94.2m);
        settled.GetProperty("net").GetProperty("unit").GetString().ShouldBe("g");
        settled.GetProperty("stable").GetBoolean().ShouldBeTrue();
        settled.GetProperty("validity").GetString().ShouldBe("valid");
    }

    [Fact]
    public async Task SodiumSulphate_OnFloorScale_ReadsInKilogramsAtThreeDecimals()
    {
        await TareAsync("scale-32kg");
        await _simulator.PostAsync("/_sim/scales/scale-32kg/scenario", new { scenario = "rising", target = new { value = 24.000m, unit = "kg" } });
        _simulator.Clock.Advance(_pour);

        var net = (await GetJsonAsync("/api/scales/scale-32kg/weight")).GetProperty("net");

        net.GetProperty("value").GetDecimal().ShouldBe(24.000m);
        net.GetProperty("unit").GetString().ShouldBe("kg");
    }

    [Fact]
    public async Task OverCapacity_IsReportedAsValidityNotError()
    {
        await TareAsync("scale-3kg");
        await _simulator.PostAsync("/_sim/scales/scale-3kg/scenario", new { scenario = "rising", target = new { value = 5m, unit = "kg" } });
        _simulator.Clock.Advance(_pour);

        (await GetJsonAsync("/api/scales/scale-3kg/weight")).GetProperty("validity").GetString().ShouldBe("overCapacity");
    }

    [Fact]
    public async Task ScaleFault_Unavailable_ReadAndTareReturn503AndStatusOffline()
    {
        await _simulator.FaultAsync("scale:scale-3kg", "unavailable");

        var read = await _client.GetAsync("/api/scales/scale-3kg/weight");
        var tare = await TareAsync("scale-3kg");
        var status = await GetJsonAsync("/api/scales/scale-3kg/status");

        read.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await read.ErrorCodeAsync()).ShouldBe("Scale.Unavailable");
        tare.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        status.GetProperty("status").GetString().ShouldBe("offline");
        (await _client.GetAsync("/api/scales/scale-32kg/weight")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ScaleFault_Error500_ReadReturns503ButDeviceStatusStaysAsReported()
    {
        await _simulator.FaultAsync("scale:scale-3kg", "error500");

        var read = await _client.GetAsync("/api/scales/scale-3kg/weight");

        read.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await read.ErrorCodeAsync()).ShouldBe("Scale.Unavailable");
        (await GetJsonAsync("/api/scales/scale-3kg/status")).GetProperty("status").GetString().ShouldBe("ready");
    }

    [Fact]
    public async Task ScaleFault_Timeout_IsCutOffByTheOneSecondAttemptTimeoutAndReturns503()
    {
        await _simulator.FaultAsync("scale:scale-3kg", "timeout");
        var started = System.Diagnostics.Stopwatch.StartNew();

        var response = await _client.GetAsync("/api/scales/scale-3kg/weight");

        started.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(5));

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await response.ErrorCodeAsync()).ShouldBe("Scale.Unavailable");
    }

    [Fact]
    public async Task ScaleComingBack_MovesOnlineSinceForward()
    {
        var before = (await GetJsonAsync("/api/scales/scale-3kg/weight")).GetProperty("onlineSince").GetDateTimeOffset();
        await _simulator.FaultAsync("scale:scale-3kg", "unavailable");
        fixture.Clock.Advance(TimeSpan.FromSeconds(5));
        (await _client.GetAsync("/api/scales/scale-3kg/weight")).StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        await _simulator.ClearFaultsAsync();
        fixture.Clock.Advance(TimeSpan.FromSeconds(5));

        var after = (await GetJsonAsync("/api/scales/scale-3kg/weight")).GetProperty("onlineSince").GetDateTimeOffset();

        after.ShouldBe(before.AddSeconds(10));
    }

    [Fact]
    public async Task UnknownScale_Returns404ScaleNotFound()
    {
        var response = await _client.GetAsync("/api/scales/scale-99/weight");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.ErrorCodeAsync()).ShouldBe("Scale.NotFound");
    }

    [Fact]
    public async Task Scanner_DeliversEachScanOncePerCursor()
    {
        await _simulator.PostAsync("/_sim/scanner/scan", new { code = "B-24187" });
        await _simulator.PostAsync("/_sim/scanner/scan", new { code = "2026-0387" });

        var first = await GetJsonAsync("/api/scanner/scans?after=0");
        var cursor = first.GetProperty("cursor").GetInt64();
        var second = await GetJsonAsync($"/api/scanner/scans?after={cursor}");

        first.GetProperty("items").EnumerateArray().Select(s => s.GetProperty("code").GetString()).ShouldBe(["B-24187", "2026-0387"]);
        cursor.ShouldBe(2);
        second.GetProperty("items").GetArrayLength().ShouldBe(0);
        second.GetProperty("cursor").GetInt64().ShouldBe(2);
    }

    [Fact]
    public async Task Scanner_Faulted_Returns503AndStatusOffline()
    {
        await _simulator.FaultAsync("scanner", "unavailable");

        var scans = await _client.GetAsync("/api/scanner/scans?after=0");

        scans.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await scans.ErrorCodeAsync()).ShouldBe("Scanner.Unavailable");
        (await GetJsonAsync("/api/scanner/status")).GetProperty("status").GetString().ShouldBe("offline");
    }

    [Fact]
    public async Task Scanner_NegativeCursor_Returns422()
    {
        (await _client.GetAsync("/api/scanner/scans?after=-1")).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Health_ListsAllDevicesAndReflectsFaultsOnTheNextCall()
    {
        var healthy = await GetJsonAsync("/api/devices/health");
        healthy.GetProperty("items").EnumerateArray().Select(i => (i.GetProperty("device").GetString(), i.GetProperty("kind").GetString(), i.GetProperty("status").GetString()))
            .ShouldBe([("scale-3kg", "scale", "ready"), ("scale-32kg", "scale", "ready"), ("scanner", "scanner", "ready"), ("printer", "printer", "ready"), ("orgatex", "orgatex", "ready")]);

        await _simulator.FaultAsync("printer", "unavailable");
        await _simulator.FaultAsync("orgatex", "unavailable");
        await _simulator.FaultAsync("scale:scale-32kg", "unavailable");
        fixture.Clock.Advance(TimeSpan.FromSeconds(3));
        var faulted = (await GetJsonAsync("/api/devices/health")).GetProperty("items").EnumerateArray()
            .ToDictionary(i => i.GetProperty("device").GetString()!, i => (Status: i.GetProperty("status").GetString(), Since: i.GetProperty("since").GetDateTimeOffset()));

        faulted["printer"].Status.ShouldBe("offline");
        faulted["orgatex"].Status.ShouldBe("offline");
        faulted["scale-32kg"].Status.ShouldBe("offline");
        faulted["scale-32kg"].Since.ShouldBe(fixture.Clock.GetUtcNow());
        faulted["scale-3kg"].Status.ShouldBe("ready");
    }

    [Theory]
    [InlineData("/api/scales")]
    [InlineData("/api/scales/scale-3kg/weight")]
    [InlineData("/api/scanner/scans?after=0")]
    [InlineData("/api/devices/health")]
    public async Task DeviceEndpoints_WithoutSession_Return401(string url)
    {
        using var anonymous = new StationClient(_api);

        (await anonymous.GetAsync(url)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tare_WithoutXsrf_Returns400()
    {
        var response = await _client.Http.PostAsync(new Uri("/api/scales/scale-3kg/tare", UriKind.Relative), null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
