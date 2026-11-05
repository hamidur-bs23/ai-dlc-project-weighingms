namespace WeighingMS.Application.Weighing;

/// <summary>
/// Scale selection from configuration (section <c>Weighing:ScaleSelection</c>, FR-8): the threshold in kg and the
/// scale assigned to each class. Validated on start; read when a session starts (A1).
/// </summary>
public sealed class ScaleSelectionOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Weighing:ScaleSelection";

    /// <summary>The threshold in kg (inclusive on the Precision side).</summary>
    public decimal ThresholdKg { get; set; }

    /// <summary>The scale for each class.</summary>
    public ScaleAssignments Assignments { get; set; } = new();
}

/// <summary>Which scale serves each class.</summary>
public sealed class ScaleAssignments
{
    /// <summary>Scale ID for the Precision class.</summary>
    public string Precision { get; set; } = string.Empty;

    /// <summary>Scale ID for the Heavy-Duty class.</summary>
    public string HeavyDuty { get; set; } = string.Empty;
}
