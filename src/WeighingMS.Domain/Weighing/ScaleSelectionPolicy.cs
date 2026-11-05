using ErrorOr;

namespace WeighingMS.Domain.Weighing;

/// <summary>
/// The configured scale-selection rule (FR-8): the threshold in kg (inclusive on the Precision side) and the scale
/// assigned to each class. Built from configuration, never from constants.
/// </summary>
public sealed record ScaleSelectionPolicy
{
    private ScaleSelectionPolicy(decimal thresholdKg, string precisionScaleId, string heavyDutyScaleId)
    {
        ThresholdKg = thresholdKg;
        PrecisionScaleId = precisionScaleId;
        HeavyDutyScaleId = heavyDutyScaleId;
    }

    /// <summary>The boundary in kg: a target at or below it goes on the Precision scale.</summary>
    public decimal ThresholdKg { get; }

    /// <summary>The scale used for the Precision class.</summary>
    public string PrecisionScaleId { get; }

    /// <summary>The scale used for the Heavy-Duty class.</summary>
    public string HeavyDutyScaleId { get; }

    /// <summary>Creates a policy: threshold above zero and a scale for each class.</summary>
    public static ErrorOr<ScaleSelectionPolicy> Create(decimal thresholdKg, string? precisionScaleId, string? heavyDutyScaleId)
    {
        if (thresholdKg <= 0m)
        {
            return Error.Validation("Weighing.InvalidScalePolicy", "The scale-selection threshold must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(precisionScaleId) || string.IsNullOrWhiteSpace(heavyDutyScaleId))
        {
            return Error.Validation("Weighing.InvalidScalePolicy", "A scale must be assigned to both the Precision and the Heavy-Duty class.");
        }

        return new ScaleSelectionPolicy(thresholdKg, precisionScaleId.Trim(), heavyDutyScaleId.Trim());
    }

    /// <summary>The scale assigned to a class.</summary>
    public string ScaleFor(ScaleClass scaleClass) => scaleClass == ScaleClass.Precision ? PrecisionScaleId : HeavyDutyScaleId;
}
