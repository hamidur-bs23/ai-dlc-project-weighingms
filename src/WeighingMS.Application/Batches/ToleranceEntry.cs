namespace WeighingMS.Application.Batches;

/// <summary>A configured tolerance for one chemical on one scale class (TOL fallback).</summary>
public sealed class ToleranceEntry
{
    /// <summary>OrgaTEX chemical ID.</summary>
    public string ChemicalId { get; set; } = string.Empty;

    /// <summary><c>precision</c> or <c>heavyDuty</c>.</summary>
    public string ScaleClass { get; set; } = string.Empty;

    /// <summary>The ± tolerance (strictly positive).</summary>
    public decimal PlusMinus { get; set; }

    /// <summary><c>g</c> or <c>kg</c>.</summary>
    public string Unit { get; set; } = string.Empty;
}
