using System.Net;

namespace WeighingMS.Simulators.Tests;

public sealed class RecipeTests(SimulatorFactory factory) : IClassFixture<SimulatorFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetRecipe_B24187_ReturnsFiveComponentsInMockupSequence()
    {
        var json = await (await _client.GetUrlAsync("/api/batches/B-24187/recipe")).ReadJsonAsync();
        var names = json.GetProperty("components").EnumerateArray().Select(c => c.GetProperty("name").GetString());

        names.ShouldBe(["Remazol Brill. Blue RSPL", "Remazol Red RB", "Remazol Yellow RGB", "Sodium Sulphate", "Soda Ash"]);
    }

    [Theory]
    [InlineData(1, "DYE-1180", "1240.0", "g", "6.0", "C-09")]
    [InlineData(2, "DYE-2204", "386.0", "g", "2.0", "C-11")]
    [InlineData(3, "DYE-4471", "94.0", "g", "0.5", "C-12")]
    [InlineData(4, "AUX-0101", "24.000", "kg", "0.120", "D-03")]
    [InlineData(5, "AUX-0107", "9.600", "kg", "0.048", "D-05")]
    public async Task GetRecipe_B24187_ComponentMatchesMockupData(int sequence, string code, string target, string unit, string tolerance, string container)
    {
        var json = await (await _client.GetUrlAsync("/api/batches/B-24187/recipe")).ReadJsonAsync();
        var c = json.GetProperty("components").EnumerateArray().Single(x => x.GetProperty("sequence").GetInt32() == sequence);

        c.GetProperty("code").GetString().ShouldBe(code);
        c.GetProperty("targetQuantity").GetDecimal().ShouldBe(decimal.Parse(target, System.Globalization.CultureInfo.InvariantCulture));
        c.GetProperty("unit").GetString().ShouldBe(unit);
        c.GetProperty("tolerance").GetDecimal().ShouldBe(decimal.Parse(tolerance, System.Globalization.CultureInfo.InvariantCulture));
        c.GetProperty("suggestedContainer").GetString().ShouldBe(container);
    }

    [Fact]
    public async Task GetRecipe_B24190_ContainsAComponentOfExactlyThreeKilograms()
    {
        var json = await (await _client.GetUrlAsync("/api/batches/B-24190/recipe")).ReadJsonAsync();

        json.GetProperty("trainingOnly").GetBoolean().ShouldBeTrue();
        json.GetProperty("components").EnumerateArray()
            .ShouldContain(c => c.GetProperty("unit").GetString() == "kg" && c.GetProperty("targetQuantity").GetDecimal() == 3.000m);
    }

    [Fact]
    public async Task GetRecipe_UnknownBatch_Returns404()
    {
        var response = await _client.GetUrlAsync("/api/batches/NOPE/recipe");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.ProblemTypeAsync()).ShouldBe("Batch.NotFound");
    }
}
