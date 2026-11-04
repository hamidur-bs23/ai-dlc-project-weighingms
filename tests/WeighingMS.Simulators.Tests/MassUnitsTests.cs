using WeighingMS.Simulators.Host.Common;

namespace WeighingMS.Simulators.Tests;

public sealed class MassUnitsTests
{
    [Theory]
    [InlineData("94.0", "g", "kg", "0.094")]
    [InlineData("3.000", "kg", "g", "3000")]
    [InlineData("24.000", "kg", "kg", "24.000")]
    public void Convert_BetweenUnits_IsExact(string value, string from, string to, string expected)
    {
        MassUnits.Convert(Parse(value), from, to).ShouldBe(Parse(expected));
    }

    [Fact]
    public void Convert_UnknownUnit_Throws()
    {
        Should.Throw<ArgumentException>(() => MassUnits.Convert(1m, "lb", "g"));
    }

    [Theory]
    [InlineData("94.24", "0.1", "94.2")]
    [InlineData("94.25", "0.1", "94.3")]
    [InlineData("24.0005", "0.001", "24.001")]
    [InlineData("23.99949", "0.001", "23.999")]
    public void RoundToResolution_RoundsMidpointsAwayFromZero(string value, string resolution, string expected)
    {
        MassUnits.RoundToResolution(Parse(value), Parse(resolution)).ShouldBe(Parse(expected));
    }

    [Fact]
    public void RoundToResolution_NonPositiveResolution_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MassUnits.RoundToResolution(1m, 0m));
    }

    private static decimal Parse(string s) => decimal.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
}
