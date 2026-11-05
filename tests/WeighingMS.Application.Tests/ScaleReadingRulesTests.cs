using WeighingMS.Application.Devices;

namespace WeighingMS.Application.Tests;

public sealed class ScaleReadingRulesTests
{
    private static readonly DateTimeOffset _at = new(2026, 11, 3, 21, 48, 22, TimeSpan.Zero);

    private static RawScaleReading Raw(string? net, string? unit = "g", bool overCapacity = false) =>
        new("scale-3kg", net is null ? null : decimal.Parse(net, System.Globalization.CultureInfo.InvariantCulture), unit, true, overCapacity, _at);

    [Theory]
    [InlineData("94.2", ReadingValidity.Valid)]
    [InlineData("0.0", ReadingValidity.Valid)]
    [InlineData("-0.5", ReadingValidity.Valid)]
    [InlineData("-0.6", ReadingValidity.BelowZero)]
    [InlineData("-214.0", ReadingValidity.BelowZero)]
    public void Classify_AgainstZeroBandOfHalfAGram(string net, ReadingValidity expected)
    {
        ScaleReadingRules.Classify(Raw(net), 0.5m).ShouldBe(expected);
    }

    [Fact]
    public void Classify_DeviceFlaggedOverCapacity_IsOverCapacity()
    {
        ScaleReadingRules.Classify(Raw("3200.0", overCapacity: true), 0.5m).ShouldBe(ReadingValidity.OverCapacity);
    }

    [Theory]
    [InlineData(null, "g")]
    [InlineData("94.2", null)]
    [InlineData("94.2", " ")]
    public void Classify_MissingValueOrUnit_IsMalformed(string? net, string? unit)
    {
        ScaleReadingRules.Classify(Raw(net, unit), 0.5m).ShouldBe(ReadingValidity.Malformed);
    }

    [Fact]
    public void Classify_MalformedWinsOverCapacityFlag()
    {
        ScaleReadingRules.Classify(Raw(null, overCapacity: true), 0.5m).ShouldBe(ReadingValidity.Malformed);
    }

    [Fact]
    public void Classify_NegativeBandIsTreatedAsItsMagnitude()
    {
        ScaleReadingRules.Classify(Raw("-0.4"), -0.5m).ShouldBe(ReadingValidity.Valid);
    }
}
