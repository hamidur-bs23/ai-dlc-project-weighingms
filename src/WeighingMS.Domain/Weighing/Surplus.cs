using WeighingMS.Domain.Common;

namespace WeighingMS.Domain.Weighing;

/// <summary>
/// How far an Overweight reading exceeds the target (FR-16, A16): amount = net − target and the percentage of the
/// target, 1 dp. Yellow RGB 96.4 g against 94.0 g → +2.4 g (+2.6%). Shown and recorded; never changes another component
/// (PD-05 option 1).
/// </summary>
public sealed record Surplus(Quantity Amount, decimal PercentOfTarget)
{
    /// <summary>The surplus of a classification, or null unless it is Overweight.</summary>
    public static Surplus? Of(Classification classification)
    {
        ArgumentNullException.ThrowIfNull(classification);
        if (classification.Class != WeightClass.Overweight)
        {
            return null;
        }

        var target = classification.Band.Target;
        var amount = classification.Net.Value - target.Value;
        var percent = Math.Round(amount / target.Value * 100m, 1, MidpointRounding.AwayFromZero);
        return new Surplus(Quantity.Of(amount, target.Unit), percent);
    }
}
