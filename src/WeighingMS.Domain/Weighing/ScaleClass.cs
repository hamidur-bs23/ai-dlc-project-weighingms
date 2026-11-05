namespace WeighingMS.Domain.Weighing;

/// <summary>The two scale classes of the PDF (§5): prototype names "3 kg precision" and "32 kg floor".</summary>
public enum ScaleClass
{
    /// <summary>Required quantity at or below the threshold.</summary>
    Precision,

    /// <summary>Required quantity above the threshold.</summary>
    HeavyDuty,
}
