using WeighingMS.Domain.Common;
using WeighingMS.Domain.Weighing;
using static WeighingMS.Domain.Tests.TestRecipes;

namespace WeighingMS.Domain.Tests;

/// <summary>FR-11 / FR-12: four classes, inclusive bounds, fixed-point, at the scale's resolution.</summary>
public sealed class ReadingClassifierTests
{
    private static readonly ScaleProfile _kgMilligramScale = new("lab", MassUnit.Kilogram, Quantity.Of(0.001m, MassUnit.Kilogram));

    // Workbook STEP 22: Yellow RGB 94.0 g ± 0.5 g on the 3 kg precision scale (0.1 g).
    [Theory]
    [InlineData(93.4, WeightClass.Underweight, false, 0.6)]
    [InlineData(93.5, WeightClass.WithinRange, true, 0.5)]
    [InlineData(94.0, WeightClass.TargetAchieved, true, 0.0)]
    [InlineData(94.5, WeightClass.WithinRange, true, 0.0)]
    [InlineData(94.6, WeightClass.Overweight, false, 0.0)]
    public void Classify_Step22YellowRgb(decimal net, WeightClass expected, bool confirmable, decimal remaining)
    {
        var c = ReadingClassifier.Classify(G(94.0m), G(0.5m), G(net), Precision);

        c.Class.ShouldBe(expected);
        c.IsConfirmable.ShouldBe(confirmable);
        c.Remaining.ShouldBe(G(remaining));
    }

    // PDF §7: 1.000 kg ± 0.010 kg.
    [Theory]
    [InlineData(0.980, WeightClass.Underweight)]
    [InlineData(0.995, WeightClass.WithinRange)]
    [InlineData(1.000, WeightClass.TargetAchieved)]
    [InlineData(1.008, WeightClass.WithinRange)]
    [InlineData(1.020, WeightClass.Overweight)]
    [InlineData(0.990, WeightClass.WithinRange)]   // lower bound is inclusive
    [InlineData(1.010, WeightClass.WithinRange)]   // upper bound is inclusive
    [InlineData(0.989, WeightClass.Underweight)]
    [InlineData(1.011, WeightClass.Overweight)]
    public void Classify_PdfExample(decimal net, WeightClass expected) =>
        ReadingClassifier.Classify(Kg(1.000m), Kg(0.010m), Kg(net), _kgMilligramScale).Class.ShouldBe(expected);

    // WEIGH-ROUNDING: a device sending more digits than the resolution is judged as the indicator shows it.
    [Theory]
    [InlineData(93.45, 93.5, WeightClass.WithinRange)]
    [InlineData(93.44, 93.4, WeightClass.Underweight)]
    [InlineData(94.55, 94.6, WeightClass.Overweight)]
    [InlineData(94.54, 94.5, WeightClass.WithinRange)]
    [InlineData(94.04, 94.0, WeightClass.TargetAchieved)]
    [InlineData(93.96, 94.0, WeightClass.TargetAchieved)]
    public void Classify_ExtraDigits_AreRoundedToTheResolutionFirst(decimal raw, decimal shown, WeightClass expected)
    {
        var c = ReadingClassifier.Classify(G(94.0m), G(0.5m), G(raw), Precision);

        c.Net.ShouldBe(G(shown));
        c.Class.ShouldBe(expected);
    }

    // Assumption A4: classified in the scale's unit; the result does not depend on the unit of the input.
    [Fact]
    public void Classify_KilogramTargetOnAGramScale_IsComparedInGrams()
    {
        var c = ReadingClassifier.Classify(Kg(2.500m), Kg(0.010m), G(2490.0m), Precision);

        c.Class.ShouldBe(WeightClass.WithinRange);
        c.Band.Target.ShouldBe(G(2500.0m));
        c.Band.Lower.ShouldBe(G(2490.0m));
        c.Band.Upper.ShouldBe(G(2510.0m));
        c.Remaining.ShouldBe(G(10.0m));
    }

    [Fact]
    public void Classify_ReadingInAnotherUnit_IsConvertedToTheScaleUnit()
    {
        var c = ReadingClassifier.Classify(Kg(24.000m), Kg(0.120m), G(24048m), Floor);

        c.Net.ShouldBe(Kg(24.048m));
        c.Class.ShouldBe(WeightClass.WithinRange);
    }

    [Fact]
    public void Classify_ToleranceInAnotherUnit_BuildsTheBandInTheScaleUnit()
    {
        var c = ReadingClassifier.Classify(Kg(9.600m), G(48m), Kg(9.648m), Floor);

        c.Band.PlusMinus.ShouldBe(Kg(0.048m));
        c.Class.ShouldBe(WeightClass.WithinRange);
    }

    // Sodium Sulphate 24.000 ± 0.120 kg on the 32 kg floor scale (0.001 kg).
    [Theory]
    [InlineData(23.879, WeightClass.Underweight, 0.121)]
    [InlineData(23.880, WeightClass.WithinRange, 0.120)]
    [InlineData(24.000, WeightClass.TargetAchieved, 0.000)]
    [InlineData(24.120, WeightClass.WithinRange, 0.000)]
    [InlineData(24.121, WeightClass.Overweight, 0.000)]
    public void Classify_SodiumSulphateOnTheFloorScale(decimal net, WeightClass expected, decimal remaining)
    {
        var c = ReadingClassifier.Classify(Kg(24.000m), Kg(0.120m), Kg(net), Floor);

        c.Class.ShouldBe(expected);
        c.Remaining.ShouldBe(Kg(remaining));
    }

    // Assumption A5: below zero is Underweight, and remaining is the full target (not more).
    [Theory]
    [InlineData(-0.3)]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    public void Classify_NothingOrLessOnTheScale_IsUnderweightWithFullTargetRemaining(decimal net)
    {
        var c = ReadingClassifier.Classify(G(94.0m), G(0.5m), G(net), Precision);

        c.Class.ShouldBe(WeightClass.Underweight);
        c.Remaining.ShouldBe(G(94.0m));
    }

    [Fact]
    public void Classify_RisingPour_RemainingFollowsTheNet()
    {
        ReadingClassifier.Classify(G(94.0m), G(0.5m), G(55.3m), Precision).Remaining.ShouldBe(G(38.7m));
    }

    [Fact]
    public void Band_IsShownAsTargetPlusMinusAndRange()
    {
        var band = ToleranceBand.Create(G(94.0m), G(0.5m), MassUnit.Gram);

        band.Target.ShouldBe(G(94.0m));
        band.PlusMinus.ShouldBe(G(0.5m));
        band.Lower.ShouldBe(G(93.5m));
        band.Upper.ShouldBe(G(94.5m));
    }
}
