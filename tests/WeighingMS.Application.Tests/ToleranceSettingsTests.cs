using WeighingMS.Application.Batches;

namespace WeighingMS.Application.Tests;

public sealed class ToleranceSettingsTests
{
    private static ToleranceEntry Entry(string chemical = "DYE-4471", string scaleClass = "precision", decimal plusMinus = 0.5m, string unit = "g") =>
        new() { ChemicalId = chemical, ScaleClass = scaleClass, PlusMinus = plusMinus, Unit = unit };

    [Fact]
    public void Validate_ValidEntries_HasNoErrors()
    {
        new ToleranceSettings { Entries = [Entry(), Entry("AUX-0101", "heavyDuty", 0.120m, "kg")] }.Validate().ShouldBeEmpty();
    }

    [Fact]
    public void Validate_NoEntries_IsValid()
    {
        new ToleranceSettings().Validate().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("DYE-4471", "precision", "0", "g", "greater than zero")]
    [InlineData("DYE-4471", "precision", "-0.5", "g", "greater than zero")]
    [InlineData("DYE-4471", "precision", "0.5", "lb", "unit must be g or kg")]
    [InlineData("DYE-4471", "floor", "0.5", "g", "scaleClass must be precision or heavyDuty")]
    [InlineData(" ", "precision", "0.5", "g", "no chemicalId")]
    public void Validate_InvalidEntry_IsReported(string chemical, string scaleClass, string plusMinus, string unit, string message)
    {
        var settings = new ToleranceSettings { Entries = [Entry(chemical, scaleClass, decimal.Parse(plusMinus, System.Globalization.CultureInfo.InvariantCulture), unit)] };

        settings.Validate().ShouldContain(e => e.Contains(message, StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_DuplicateChemicalAndScaleClass_IsReported()
    {
        new ToleranceSettings { Entries = [Entry(), Entry(plusMinus: 0.4m)] }.Validate().ShouldContain(e => e.Contains("duplicate", StringComparison.Ordinal));
    }
}
