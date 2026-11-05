using WeighingMS.Domain.Common;

namespace WeighingMS.Domain.Weighing;

/// <summary>
/// Classifies a net reading against a component's target and tolerance (FR-11, FR-12). Pure and fixed-point:
/// everything is expressed in the scale's unit (assumption A4) and compared at the scale's resolution.
/// </summary>
public static class ReadingClassifier
{
    /// <summary>
    /// 1. Net, target and band in the scale's unit; net and target rounded to the resolution (WEIGH-ROUNDING).
    /// 2. Net &lt; lower → Underweight; net &gt; upper → Overweight; net = target → Target achieved; else Within range.
    /// Remaining = max(0, target − max(0, net)): a negative net counts as nothing weighed (assumption A5).
    /// </summary>
    public static Classification Classify(Quantity target, Quantity plusMinus, Quantity net, ScaleProfile scale)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(plusMinus);
        ArgumentNullException.ThrowIfNull(net);
        ArgumentNullException.ThrowIfNull(scale);

        var unit = scale.Unit;
        var band = ToleranceBand.Create(target.RoundTo(scale.Resolution), plusMinus, unit);
        var measured = net.ConvertTo(unit).RoundTo(scale.Resolution);

        var weightClass = ClassOf(measured.Value, band);
        var remaining = Quantity.Of(Math.Max(0m, band.Target.Value - Math.Max(0m, measured.Value)), unit);
        return new Classification(weightClass, measured, remaining, band);
    }

    /// <summary>The class of a measured value against an inclusive band (both bounds belong to the band).</summary>
    private static WeightClass ClassOf(decimal net, ToleranceBand band)
    {
        if (net < band.Lower.Value)
        {
            return WeightClass.Underweight;
        }

        if (net > band.Upper.Value)
        {
            return WeightClass.Overweight;
        }

        return net == band.Target.Value ? WeightClass.TargetAchieved : WeightClass.WithinRange;
    }
}
