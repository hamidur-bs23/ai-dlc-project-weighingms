using WeighingMS.Domain.Common;
using WeighingMS.Domain.Weighing;
using static WeighingMS.Domain.Tests.TestRecipes;

namespace WeighingMS.Domain.Tests;

/// <summary>FR-8 / workbook STEP 21: ≤ 3 kg (inclusive) → Precision, &gt; 3 kg → Heavy-Duty, compared in kg.</summary>
public sealed class ScaleSelectorTests
{
    [Theory]
    [InlineData(2.999, "kg", ScaleClass.Precision, "scale-3kg")]
    [InlineData(3.000, "kg", ScaleClass.Precision, "scale-3kg")]
    [InlineData(3.001, "kg", ScaleClass.HeavyDuty, "scale-32kg")]
    [InlineData(2999, "g", ScaleClass.Precision, "scale-3kg")]
    [InlineData(3000, "g", ScaleClass.Precision, "scale-3kg")]
    [InlineData(3001, "g", ScaleClass.HeavyDuty, "scale-32kg")]
    [InlineData(94.0, "g", ScaleClass.Precision, "scale-3kg")]      // Yellow RGB → "3 kg precision"
    [InlineData(24.000, "kg", ScaleClass.HeavyDuty, "scale-32kg")]  // Sodium Sulphate → "32 kg floor"
    public void Select_DefaultThreshold_PicksTheClassAfterConversion(decimal value, string unit, ScaleClass expected, string scaleId)
    {
        var selection = ScaleSelector.Select(Quantity.Create(value, unit).Value, DefaultPolicy);

        selection.ScaleClass.ShouldBe(expected);
        selection.ScaleId.ShouldBe(scaleId);
    }

    [Fact]
    public void Select_ConfiguredThreshold_IsUsedInsteadOfThreeKilograms()
    {
        var policy = ScaleSelectionPolicy.Create(2.0m, "precision-b", "floor-b").Value;

        ScaleSelector.Select(Kg(2.0m), policy).ShouldBe(new ScaleSelection(ScaleClass.Precision, "precision-b"));
        ScaleSelector.Select(Kg(2.5m), policy).ShouldBe(new ScaleSelection(ScaleClass.HeavyDuty, "floor-b"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreatePolicy_ThresholdNotPositive_IsRejected(decimal threshold) =>
        ScaleSelectionPolicy.Create(threshold, "scale-3kg", "scale-32kg").FirstError.Code.ShouldBe("Weighing.InvalidScalePolicy");

    [Theory]
    [InlineData("", "scale-32kg")]
    [InlineData("scale-3kg", " ")]
    [InlineData(null, "scale-32kg")]
    public void CreatePolicy_MissingAssignment_IsRejected(string? precision, string? heavyDuty) =>
        ScaleSelectionPolicy.Create(3m, precision, heavyDuty).FirstError.Code.ShouldBe("Weighing.InvalidScalePolicy");

    [Fact]
    public void CreatePolicy_TrimsScaleIds() =>
        ScaleSelectionPolicy.Create(3m, " scale-3kg ", "scale-32kg").Value.ScaleFor(ScaleClass.Precision).ShouldBe("scale-3kg");
}
