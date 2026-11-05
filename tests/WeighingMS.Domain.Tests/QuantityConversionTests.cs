using WeighingMS.Domain.Common;
using static WeighingMS.Domain.Tests.TestRecipes;

namespace WeighingMS.Domain.Tests;

public sealed class QuantityConversionTests
{
    [Theory]
    [InlineData(94.0, 0.094)]
    [InlineData(3000.0, 3.0)]
    [InlineData(2999.0, 2.999)]
    [InlineData(0.1, 0.0001)]
    public void ConvertTo_GramsToKilograms_IsExact(decimal grams, decimal kilograms)
    {
        var converted = G(grams).ConvertTo(MassUnit.Kilogram);

        converted.Value.ShouldBe(kilograms);
        converted.Unit.ShouldBe(MassUnit.Kilogram);
        converted.ConvertTo(MassUnit.Gram).Value.ShouldBe(grams);
    }

    [Fact]
    public void ConvertTo_SameUnit_ReturnsTheSameQuantity()
    {
        var q = Kg(24.000m);
        q.ConvertTo(MassUnit.Kilogram).ShouldBeSameAs(q);
    }

    // WEIGH-ROUNDING: half away from zero, at the scale resolution.
    [Theory]
    [InlineData(93.45, 93.5)]
    [InlineData(93.44, 93.4)]
    [InlineData(94.55, 94.6)]
    [InlineData(94.04, 94.0)]
    [InlineData(94.0, 94.0)]
    [InlineData(-0.35, -0.4)]
    public void RoundTo_TenthOfAGram_IsHalfAwayFromZero(decimal raw, decimal expected) =>
        G(raw).RoundTo(G(0.1m)).Value.ShouldBe(expected);

    [Theory]
    [InlineData(24.0485, 24.049)]
    [InlineData(24.0484, 24.048)]
    [InlineData(1.0005, 1.001)]
    public void RoundTo_OneGramOnAKilogramScale_IsHalfAwayFromZero(decimal raw, decimal expected) =>
        Kg(raw).RoundTo(Kg(0.001m)).Value.ShouldBe(expected);

    [Fact]
    public void RoundTo_StepInAnotherUnit_IsConvertedFirst() =>
        Kg(1.23456m).RoundTo(G(1m)).Value.ShouldBe(1.235m);

    [Fact]
    public void RoundTo_ZeroStep_IsRejected() =>
        Should.Throw<ArgumentOutOfRangeException>(() => G(1m).RoundTo(G(0m)));

    [Theory]
    [InlineData(MassUnit.Gram, "g")]
    [InlineData(MassUnit.Kilogram, "kg")]
    public void SymbolOf_IsTheWireSymbol(MassUnit unit, string symbol) => Quantity.SymbolOf(unit).ShouldBe(symbol);
}
