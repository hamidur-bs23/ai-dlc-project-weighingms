namespace WeighingMS.Application.Batches;

/// <summary>
/// Configured tolerances per chemical + scale class (section <c>Tolerances</c>). Used only when the recipe gives no
/// tolerance; there is no universal default (PDF §6 Step 6, decision TOL).
/// </summary>
public sealed class ToleranceSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Tolerances";

    /// <summary>Scale classes an entry may name.</summary>
    public static readonly IReadOnlySet<string> ScaleClasses = new HashSet<string>(StringComparer.Ordinal) { "precision", "heavyDuty" };

    /// <summary>The entries.</summary>
    public IReadOnlyList<ToleranceEntry> Entries { get; set; } = [];

    /// <summary>Validation errors (empty when valid): positive tolerance, known unit and scale class, no duplicates.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        foreach (var e in Entries)
        {
            if (string.IsNullOrWhiteSpace(e.ChemicalId))
            {
                errors.Add("an entry has no chemicalId");
            }

            if (e.PlusMinus <= 0m)
            {
                errors.Add($"{e.ChemicalId}/{e.ScaleClass}: tolerance must be greater than zero");
            }

            if (e.Unit is not ("g" or "kg"))
            {
                errors.Add($"{e.ChemicalId}/{e.ScaleClass}: unit must be g or kg");
            }

            if (!ScaleClasses.Contains(e.ScaleClass))
            {
                errors.Add($"{e.ChemicalId}: scaleClass must be precision or heavyDuty");
            }
        }

        errors.AddRange(Entries.GroupBy(e => (e.ChemicalId, e.ScaleClass)).Where(g => g.Count() > 1)
            .Select(g => $"{g.Key.ChemicalId}/{g.Key.ScaleClass}: duplicate entry"));
        return errors;
    }
}
