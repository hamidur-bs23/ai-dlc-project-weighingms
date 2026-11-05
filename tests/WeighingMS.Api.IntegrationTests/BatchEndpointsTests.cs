using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace WeighingMS.Api.IntegrationTests;

/// <summary>The STEP 19 batch flow through the Api, against the real simulator in-process and PostgreSQL 17.</summary>
public sealed class BatchEndpointsTests(ApiFixture fixture) : IAsyncLifetime
{
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return await response.ReadJsonAsync();
    }

    private static Dictionary<string, string?> StatusByBatch(JsonElement queue) =>
        queue.GetProperty("items").EnumerateArray().ToDictionary(b => b.GetProperty("batchId").GetString()!, b => b.GetProperty("status").GetString());

    [Fact]
    public async Task Step19Flow_QueueThenBatchThenRecipe()
    {
        var queue = await GetJsonAsync("/api/batches");
        var first = queue.GetProperty("items")[0];
        queue.GetProperty("items").EnumerateArray().Select(b => b.GetProperty("batchId").GetString()).ShouldBe(["B-24187", "B-24188", "B-24190", "B-24191"]);
        first.GetProperty("orderId").GetString().ShouldBe("SO-9931");
        first.GetProperty("machine").GetString().ShouldBe("JET-04");
        first.GetProperty("fabric").GetString().ShouldBe("100% CTN knit");
        first.GetProperty("batchWeight").GetProperty("value").GetDecimal().ShouldBe(480m);
        first.GetProperty("batchWeight").GetProperty("unit").GetString().ShouldBe("kg");
        first.GetProperty("shade").GetString().ShouldBe("Teal 4C");
        first.GetProperty("componentCount").GetInt32().ShouldBe(5);
        first.GetProperty("trainingOnly").GetBoolean().ShouldBeFalse();
        StatusByBatch(queue).Values.ShouldAllBe(s => s == "queued");

        var batch = await GetJsonAsync("/api/batches/B-24187");
        batch.GetProperty("shade").GetString().ShouldBe("Teal 4C");

        var recipe = await GetJsonAsync("/api/batches/B-24187/recipe");
        var components = recipe.GetProperty("components").EnumerateArray().ToList();
        components.Select(c => c.GetProperty("code").GetString()).ShouldBe(["DYE-1180", "DYE-2204", "DYE-4471", "AUX-0101", "AUX-0107"]);
        var yellow = components[2];
        yellow.GetProperty("target").GetProperty("value").GetDecimal().ShouldBe(94.0m);
        yellow.GetProperty("target").GetProperty("unit").GetString().ShouldBe("g");
        yellow.GetProperty("tolerance").GetProperty("value").GetDecimal().ShouldBe(0.5m);
        yellow.GetProperty("suggestedContainer").GetString().ShouldBe("C-12");
        components[3].GetProperty("target").GetProperty("unit").GetString().ShouldBe("kg");

        var after = StatusByBatch(await GetJsonAsync("/api/batches"));
        after["B-24187"].ShouldBe("weighing");
        after["B-24188"].ShouldBe("queued");
    }

    [Fact]
    public async Task Recipe_IsSnapshottedOnceAndServedUnchanged()
    {
        var first = await GetJsonAsync("/api/batches/B-24187/recipe");
        fixture.Clock.Advance(TimeSpan.FromMinutes(5));
        var second = await GetJsonAsync("/api/batches/B-24187/recipe");

        second.GetProperty("loadedAt").GetDateTimeOffset().ShouldBe(first.GetProperty("loadedAt").GetDateTimeOffset());
        await using var db = fixture.NewDbContext();
        (await db.RecipeSnapshots.CountAsync(s => s.BatchId == "B-24187", TestContext.Current.CancellationToken)).ShouldBe(1);
        var stored = await db.RecipeSnapshots.Include("_components").SingleAsync(TestContext.Current.CancellationToken);
        stored.Components.Count.ShouldBe(5);
    }

    [Fact]
    public async Task OrgatexDown_SnapshottedRecipeServed_OthersUnavailableAndNothingStored_RetryWorks()
    {
        var snapshotted = await GetJsonAsync("/api/batches/B-24187/recipe");
        await _simulator.FaultAsync("orgatex", "unavailable");

        var queue = await _client.GetAsync("/api/batches");
        queue.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await queue.ErrorCodeAsync()).ShouldBe("OrgaTex.Unavailable");

        var served = await GetJsonAsync("/api/batches/B-24187/recipe");
        served.GetProperty("loadedAt").GetDateTimeOffset().ShouldBe(snapshotted.GetProperty("loadedAt").GetDateTimeOffset());

        var notYet = await _client.GetAsync("/api/batches/B-24188/recipe");
        notYet.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await notYet.ErrorCodeAsync()).ShouldBe("OrgaTex.Unavailable");
        await using (var db = fixture.NewDbContext())
        {
            (await db.RecipeSnapshots.AnyAsync(s => s.BatchId == "B-24188", TestContext.Current.CancellationToken)).ShouldBeFalse();
        }

        await _simulator.ClearFaultsAsync();
        (await _client.GetAsync("/api/batches")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _client.GetAsync("/api/batches/B-24188/recipe")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/api/batches/B-99999")]
    [InlineData("/api/batches/B-99999/recipe")]
    public async Task UnknownBatch_Returns404BatchNotFound(string url)
    {
        var response = await _client.GetAsync(url);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.ErrorCodeAsync()).ShouldBe("Batch.NotFound");
    }

    [Fact]
    public async Task B24190_ThreeKilogramComponent_IsStoredExactly()
    {
        var recipe = await GetJsonAsync("/api/batches/B-24190/recipe");

        recipe.GetProperty("trainingOnly").GetBoolean().ShouldBeTrue();
        recipe.GetProperty("components").GetArrayLength().ShouldBe(7);
        await using var db = fixture.NewDbContext();
        var stored = await db.RecipeSnapshots.Include("_components").SingleAsync(s => s.BatchId == "B-24190", TestContext.Current.CancellationToken);
        var blackB = stored.Components.Single(c => c.Code == "DYE-5502");
        blackB.Target.Value.ShouldBe(3.000m);
        blackB.Target.InKilograms.ShouldBe(3m);
        blackB.Target.Symbol.ShouldBe("kg");
    }

    [Fact]
    public async Task ConcurrentFirstCalls_StoreExactlyOneSnapshot()
    {
        var calls = Enumerable.Range(0, 6).Select(_ => _client.GetAsync("/api/batches/B-24191/recipe")).ToList();
        var responses = await Task.WhenAll(calls);

        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.OK);
        var loadedAts = new List<DateTimeOffset>();
        foreach (var r in responses)
        {
            loadedAts.Add((await r.ReadJsonAsync()).GetProperty("loadedAt").GetDateTimeOffset());
        }

        loadedAts.Distinct().Count().ShouldBe(1);
        await using var db = fixture.NewDbContext();
        (await db.RecipeSnapshots.CountAsync(s => s.BatchId == "B-24191", TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Theory]
    [InlineData("/api/batches")]
    [InlineData("/api/batches/B-24187")]
    [InlineData("/api/batches/B-24187/recipe")]
    public async Task BatchEndpoints_WithoutSession_Return401(string url)
    {
        using var anonymous = new StationClient(_api);

        (await anonymous.GetAsync(url)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
