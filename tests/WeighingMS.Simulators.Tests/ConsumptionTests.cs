using System.Net;
using System.Net.Http.Json;

namespace WeighingMS.Simulators.Tests;

public sealed class ConsumptionTests : IDisposable
{
    private readonly SimulatorFactory _factory = new();
    private readonly HttpClient _client;

    public ConsumptionTests() => _client = _factory.CreateClient();

    private static readonly object _body = new
    {
        lines = new[] { new { chemicalId = "DYE-4471", actualQuantity = 94.2m, unit = "g", lot = "2026-0387" } },
    };

    private Task<HttpResponseMessage> PostAsync(string batchId, string? key, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/batches/{batchId}/consumption")
        {
            Content = JsonContent.Create(body),
        };
        if (key is not null)
        {
            request.Headers.Add("Idempotency-Key", key);
        }

        return _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Post_ValidConsumption_Returns200AndReceipt()
    {
        var response = await PostAsync("B-24187", "key-1", _body);
        var json = await response.ReadJsonAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        json.GetProperty("batchId").GetString().ShouldBe("B-24187");
        json.GetProperty("idempotencyKey").GetString().ShouldBe("key-1");
        json.GetProperty("lines")[0].GetProperty("actualQuantity").GetDecimal().ShouldBe(94.2m);
    }

    [Fact]
    public async Task Post_RepeatedKey_ReturnsOriginalReceiptAndStoresOnce()
    {
        var first = await (await PostAsync("B-24187", "key-2", _body)).ReadJsonAsync();
        _factory.Clock.Advance(TimeSpan.FromSeconds(5));
        var second = await PostAsync("B-24187", "key-2", _body);
        var inspection = await (await _client.GetUrlAsync("/_sim/orgatex/consumption")).ReadJsonAsync();

        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await second.ReadJsonAsync()).GetProperty("receivedAt").GetString().ShouldBe(first.GetProperty("receivedAt").GetString());
        inspection.EnumerateArray().Count(r => r.GetProperty("idempotencyKey").GetString() == "key-2").ShouldBe(1);
    }

    [Fact]
    public async Task Post_MissingKey_Returns400()
    {
        var response = await PostAsync("B-24187", null, _body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ProblemTypeAsync()).ShouldBe("Consumption.IdempotencyKeyMissing");
    }

    [Fact]
    public async Task Post_UnknownBatch_Returns404()
    {
        var response = await PostAsync("B-00000", "key-3", _body);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_KeyReusedForAnotherBatch_Returns409()
    {
        await PostAsync("B-24187", "key-4", _body);
        var response = await PostAsync("B-24188", "key-4", _body);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.ProblemTypeAsync()).ShouldBe("Consumption.IdempotencyKeyConflict");
    }

    [Fact]
    public async Task Post_EmptyLines_Returns400()
    {
        var response = await PostAsync("B-24187", "key-5", new { lines = Array.Empty<object>() });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Inspection_AfterTwoPosts_ListsEachOnce()
    {
        await PostAsync("B-24187", "key-6", _body);
        await PostAsync("B-24188", "key-7", _body);

        var inspection = await (await _client.GetUrlAsync("/_sim/orgatex/consumption")).ReadJsonAsync();

        inspection.EnumerateArray().Select(r => r.GetProperty("idempotencyKey").GetString()).ShouldBe(["key-6", "key-7"], ignoreOrder: true);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
