using System.Net;
using System.Net.Http.Json;

namespace WeighingMS.Simulators.Tests;

public sealed class FaultInjectionTests : IDisposable
{
    private static readonly TimeSpan _faultTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _slowDelay = TimeSpan.FromSeconds(2);
    private readonly SimulatorFactory _factory = new();
    private readonly HttpClient _client;

    public FaultInjectionTests() => _client = _factory.CreateClient();

    private async Task InjectAsync(string target, string mode, int? durationMs = null) =>
        (await _client.PostJsonAsync("/_sim/faults", new { target, mode, durationMs })).StatusCode.ShouldBe(HttpStatusCode.NoContent);

    /// <summary>The call each target's faultable training contract answers.</summary>
    private Task<HttpResponseMessage> CallAsync(string target)
    {
        var ct = TestContext.Current.CancellationToken;
        return target switch
        {
            "orgatex" => _client.GetAsync(new Uri("/api/batches", UriKind.Relative), ct),
            "printer" => _client.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, "/api/tickets")
                {
                    Headers = { { "Idempotency-Key", Guid.NewGuid().ToString() } },
                    Content = JsonContent.Create(new { batchId = "B-24187", isReprint = false, lines = new[] { "x" } }),
                },
                ct),
            "scanner" => _client.GetAsync(new Uri("/api/scanner/scans", UriKind.Relative), ct),
            _ => _client.GetAsync(new Uri("/api/scales/scale-3kg/weight", UriKind.Relative), ct),
        };
    }

    /// <summary>Advances the fake clock in small steps until the in-flight request completes.</summary>
    private async Task<HttpResponseMessage> CompleteWhileAdvancingAsync(Task<HttpResponseMessage> call, TimeSpan total)
    {
        var step = TimeSpan.FromMilliseconds(250);
        var advanced = TimeSpan.Zero;
        while (!call.IsCompleted && advanced <= total + TimeSpan.FromSeconds(5))
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
            _factory.Clock.Advance(step);
            advanced += step;
        }

        return await call;
    }

    [Theory]
    [InlineData("orgatex", "OrgaTex.Unavailable")]
    [InlineData("scale:scale-3kg", "Scale.Unavailable")]
    [InlineData("printer", "Printer.Unavailable")]
    [InlineData("scanner", "Scanner.Unavailable")]
    public async Task Unavailable_EachTarget_Returns503WithAreaCode(string target, string code)
    {
        await InjectAsync(target, "unavailable");

        var response = await CallAsync(target);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await response.ProblemTypeAsync()).ShouldBe(code);
    }

    [Theory]
    [InlineData("orgatex")]
    [InlineData("scale:scale-3kg")]
    [InlineData("printer")]
    [InlineData("scanner")]
    public async Task Error500_EachTarget_Returns500(string target)
    {
        await InjectAsync(target, "error500");

        var response = await CallAsync(target);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        (await response.ProblemTypeAsync()).ShouldBe("Simulator.InternalError");
    }

    [Theory]
    [InlineData("orgatex")]
    [InlineData("scale:scale-3kg")]
    [InlineData("printer")]
    [InlineData("scanner")]
    public async Task Timeout_EachTarget_HoldsForFaultTimeoutThenReturns504(string target)
    {
        await InjectAsync(target, "timeout");

        var call = CallAsync(target);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        call.IsCompleted.ShouldBeFalse();
        var response = await CompleteWhileAdvancingAsync(call, _faultTimeout);

        response.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
        (await response.ProblemTypeAsync()).ShouldBe("Simulator.Timeout");
    }

    [Theory]
    [InlineData("orgatex")]
    [InlineData("scale:scale-3kg")]
    [InlineData("printer")]
    [InlineData("scanner")]
    public async Task Slow_EachTarget_DelaysThenServesNormally(string target)
    {
        await InjectAsync(target, "slow");

        var call = CallAsync(target);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        call.IsCompleted.ShouldBeFalse();
        var response = await CompleteWhileAdvancingAsync(call, _slowDelay);

        response.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Fact]
    public async Task ScaleFault_AffectsOnlyThatScale()
    {
        await InjectAsync("scale:scale-3kg", "unavailable");

        (await _client.GetUrlAsync("/api/scales/scale-3kg/weight")).StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await _client.GetUrlAsync("/api/scales/scale-32kg/weight")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _client.PostJsonAsync("/api/scales/scale-3kg/tare", new { })).StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Theory]
    [InlineData("scale:scale-3kg", "/api/scales/scale-3kg/status")]
    [InlineData("printer", "/api/printer/status")]
    [InlineData("scanner", "/api/scanner/status")]
    public async Task Unavailable_StatusEndpoint_ReportsOfflineInsteadOfFailing(string target, string statusUrl)
    {
        await InjectAsync(target, "unavailable");

        var response = await _client.GetUrlAsync(statusUrl);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadJsonAsync()).GetProperty("status").GetString().ShouldBe("OFFLINE");
    }

    [Fact]
    public async Task Unavailable_ScaleList_ShowsTheFaultedScaleOffline()
    {
        await InjectAsync("scale:scale-32kg", "unavailable");

        var list = await (await _client.GetUrlAsync("/api/scales")).ReadJsonAsync();
        var status = list.EnumerateArray().ToDictionary(s => s.GetProperty("scaleId").GetString()!, s => s.GetProperty("status").GetString());

        status["scale-32kg"].ShouldBe("OFFLINE");
        status["scale-3kg"].ShouldBe("READY");
    }

    [Fact]
    public async Task Fault_WithDuration_ClearsByItself()
    {
        await InjectAsync("orgatex", "unavailable", durationMs: 5000);
        (await CallAsync("orgatex")).StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);

        _factory.Clock.Advance(TimeSpan.FromSeconds(5));

        (await CallAsync("orgatex")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await (await _client.GetUrlAsync("/_sim/faults")).ReadJsonAsync()).GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task List_ShowsActiveFaultsWithExpiry()
    {
        await InjectAsync("printer", "error500", durationMs: 1500);
        await InjectAsync("scale:scale-3kg", "unavailable");

        var faults = await (await _client.GetUrlAsync("/_sim/faults")).ReadJsonAsync();

        faults.EnumerateArray().Select(f => f.GetProperty("target").GetString()).ShouldBe(["printer", "scale:scale-3kg"]);
        faults[0].GetProperty("mode").GetString().ShouldBe("error500");
    }

    [Fact]
    public async Task NewFault_ReplacesThePreviousOneOnTheSameTarget()
    {
        await InjectAsync("orgatex", "unavailable");
        await InjectAsync("orgatex", "error500");

        (await CallAsync("orgatex")).StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Delete_ClearsAllFaults()
    {
        await InjectAsync("orgatex", "unavailable");
        await InjectAsync("printer", "unavailable");

        (await _client.DeleteAsync(new Uri("/_sim/faults", UriKind.Relative), TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await CallAsync("orgatex")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await CallAsync("printer")).StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData("scale:nope")]
    [InlineData("database")]
    public async Task Inject_UnknownTarget_Returns400(string target)
    {
        var response = await _client.PostJsonAsync("/_sim/faults", new { target, mode = "unavailable" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ProblemTypeAsync()).ShouldBe("Fault.UnknownTarget");
    }

    [Fact]
    public async Task Inject_UnknownMode_Returns400()
    {
        (await _client.PostJsonAsync("/_sim/faults", new { target = "orgatex", mode = "melt" })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
