using WeighingMS.Domain.Common;

namespace WeighingMS.Domain.Tests;

public sealed class QuantityTests
{
    [Theory]
    [InlineData("94.0", "g", MassUnit.Gram)]
    [InlineData("3.000", "kg", MassUnit.Kilogram)]
    public void Create_KnownUnit_KeepsValueAndUnit(string value, string unit, MassUnit expected)
    {
        var quantity = Quantity.Create(D(value), unit).Value;

        quantity.Value.ShouldBe(D(value));
        quantity.Unit.ShouldBe(expected);
        quantity.Symbol.ShouldBe(unit);
    }

    [Theory]
    [InlineData("lb")]
    [InlineData("G")]
    [InlineData(null)]
    public void Create_UnknownUnit_IsUnknownUnitError(string? unit)
    {
        Quantity.Create(1m, unit).FirstError.Code.ShouldBe("Quantity.UnknownUnit");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.1")]
    public void CreatePositive_ZeroOrNegative_IsRejected(string value)
    {
        Quantity.CreatePositive(D(value), "g").FirstError.Code.ShouldBe("Quantity.NotPositive");
    }

    [Theory]
    [InlineData("94.0", "g", "0.094")]
    [InlineData("3000", "g", "3")]
    [InlineData("3.000", "kg", "3.000")]
    public void InKilograms_IsExact(string value, string unit, string kilograms)
    {
        Quantity.Create(D(value), unit).Value.InKilograms.ShouldBe(D(kilograms));
    }

    [Fact]
    public void Quantities_WithSameValueAndUnit_AreEqual()
    {
        Quantity.Create(94.0m, "g").Value.ShouldBe(Quantity.Create(94.0m, "g").Value);
        Quantity.Create(94.0m, "g").Value.ShouldNotBe(Quantity.Create(94.0m, "kg").Value);
        Quantity.Create(94.0m, "g").Value.ToString().ShouldBe("94.0 g");
    }

    private static decimal D(string s) => decimal.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
}
