using System.Net;

namespace WeighingMS.Simulators.Tests;

public sealed class BatchQueueTests(SimulatorFactory factory) : IClassFixture<SimulatorFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetBatches_Seed_ReturnsTheFourMockupBatchesInOrder()
    {
        var json = await (await _client.GetUrlAsync("/api/batches")).ReadJsonAsync();

        json.EnumerateArray().Select(b => b.GetProperty("batchId").GetString())
            .ShouldBe(["B-24187", "B-24188", "B-24190", "B-24191"]);
    }

    [Fact]
    public async Task GetBatches_B24187_HasTheMockupQueueFields()
    {
        var json = await (await _client.GetUrlAsync("/api/batches")).ReadJsonAsync();
        var b = json.EnumerateArray().First(x => x.GetProperty("batchId").GetString() == "B-24187");

        b.GetProperty("orderId").GetString().ShouldBe("SO-9931");
        b.GetProperty("machine").GetString().ShouldBe("JET-04");
        b.GetProperty("fabric").GetString().ShouldBe("100% CTN knit");
        b.GetProperty("batchWeightKg").GetDecimal().ShouldBe(480m);
        b.GetProperty("shade").GetString().ShouldBe("Teal 4C");
        b.GetProperty("componentCount").GetInt32().ShouldBe(5);
        b.GetProperty("status").GetString().ShouldBe("QUEUED");
        b.GetProperty("trainingOnly").GetBoolean().ShouldBeFalse();
    }

    [Theory]
    [InlineData("B-24188", "JET-07", "Teal 4C", 5)]
    [InlineData("B-24190", "JET-02", "Navy 12", 7)]
    [InlineData("B-24191", "WINCH-01", "Rose 08", 4)]
    public async Task GetBatch_TrainingBatches_MatchMockupRowsAndAreFlaggedTrainingOnly(string id, string machine, string shade, int count)
    {
        var response = await _client.GetUrlAsync($"/api/batches/{id}");
        var b = await response.ReadJsonAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        b.GetProperty("machine").GetString().ShouldBe(machine);
        b.GetProperty("shade").GetString().ShouldBe(shade);
        b.GetProperty("componentCount").GetInt32().ShouldBe(count);
        b.GetProperty("trainingOnly").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task GetBatch_UnknownId_Returns404BatchNotFound()
    {
        var response = await _client.GetUrlAsync("/api/batches/B-00000");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.ProblemTypeAsync()).ShouldBe("Batch.NotFound");
    }
}
