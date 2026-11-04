using System.Net;
using WeighingMS.Application.Access;
using WeighingMS.Infrastructure.Access;

namespace WeighingMS.Api.IntegrationTests;

public sealed class StationClockTests(ApiFixture fixture) : IAsyncLifetime
{
    private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _api = fixture.CreateApi();

    public async ValueTask InitializeAsync() => await fixture.ResetAsync();

    public async ValueTask DisposeAsync() => await _api.DisposeAsync();

    [Fact]
    public async Task Station_ReturnsUtcAndPlantTimeInAsiaDhaka()
    {
        using var client = new StationClient(_api);
        await client.SignInAsync();

        var response = await client.GetAsync("/api/station");
        var json = await response.ReadJsonAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        json.GetProperty("stationId").GetString().ShouldBe("STATION 01");
        json.GetProperty("timeZone").GetString().ShouldBe("Asia/Dhaka");
        json.GetProperty("utcNow").GetDateTimeOffset().ShouldBe(new DateTimeOffset(2026, 11, 3, 21, 48, 22, TimeSpan.Zero));
        json.GetProperty("plantNow").GetString()!.ShouldStartWith("2026-11-04T03:48:22");
        json.GetProperty("plantNow").GetString()!.ShouldEndWith("+06:00");
    }

    [Theory]
    [InlineData("Asia/Dhaka", "2026-11-04 03:48")]
    [InlineData("Europe/Berlin", "2026-11-03 22:48")]
    [InlineData("UTC", "2026-11-03 21:48")]
    public void PlantClock_ConvertsTheFr22ExampleIndependentlyOfTheServerZone(string zone, string expected)
    {
        var clock = new PlantClock(TimeProvider.System, new StationAccessOptions { TimeZone = zone });

        var local = clock.ToPlantTime(new DateTimeOffset(2026, 11, 3, 21, 48, 22, TimeSpan.Zero));

        local.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture).ShouldBe(expected);
    }

    [Fact]
    public void PlantClock_UnknownZone_FailsAtConstruction()
    {
        Should.Throw<InvalidOperationException>(() => new PlantClock(TimeProvider.System, new StationAccessOptions { TimeZone = "Mars/Olympus" }));
    }
}
