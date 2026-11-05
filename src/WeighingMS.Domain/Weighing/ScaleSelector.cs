using WeighingMS.Domain.Common;

namespace WeighingMS.Domain.Weighing;

/// <summary>Automatic scale selection (FR-8, PDF §5): compared in kg after conversion; the threshold is inclusive.</summary>
public static class ScaleSelector
{
    /// <summary>2.999 kg and 3.000 kg → Precision; 3.001 kg → Heavy-Duty (with the default 3 kg threshold).</summary>
    public static ScaleSelection Select(Quantity target, ScaleSelectionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(policy);
        var scaleClass = target.InKilograms <= policy.ThresholdKg ? ScaleClass.Precision : ScaleClass.HeavyDuty;
        return new ScaleSelection(scaleClass, policy.ScaleFor(scaleClass));
    }
}
