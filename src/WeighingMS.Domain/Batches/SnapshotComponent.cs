using WeighingMS.Domain.Common;

namespace WeighingMS.Domain.Batches;

/// <summary>One recipe line of a <see cref="RecipeSnapshot"/>. Immutable once created.</summary>
public sealed class SnapshotComponent
{
    private decimal _targetValue;
    private MassUnit _targetUnit;
    private decimal? _toleranceValue;

    private SnapshotComponent()
    {
        ChemicalId = string.Empty;
        Code = string.Empty;
        Name = string.Empty;
        DisplayColour = string.Empty;
    }

    /// <summary>Identifier (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>Weighing sequence in the recipe (1-based).</summary>
    public int Sequence { get; private set; }

    /// <summary>OrgaTEX chemical identifier.</summary>
    public string ChemicalId { get; private set; }

    /// <summary>Chemical code shown on screen.</summary>
    public string Code { get; private set; }

    /// <summary>Chemical name.</summary>
    public string Name { get; private set; }

    /// <summary>Colour swatch for the recipe rail.</summary>
    public string DisplayColour { get; private set; }

    /// <summary>Suggested container ID (OQ-1), if OrgaTEX gave one.</summary>
    public string? SuggestedContainer { get; private set; }

    /// <summary>Required quantity.</summary>
    public Quantity Target => Quantity.Create(_targetValue, _targetUnit == MassUnit.Gram ? "g" : "kg").Value;

    /// <summary>The recipe's tolerance (±), in the target's unit, or null if the recipe gave none (TOL).</summary>
    public Quantity? RecipeTolerance => _toleranceValue is { } t ? Quantity.Create(t, Target.Symbol).Value : null;

    internal static SnapshotComponent From(RecipeLine line, Quantity target, Quantity? tolerance) => new()
    {
        Id = Guid.CreateVersion7(),
        Sequence = line.Sequence,
        ChemicalId = line.ChemicalId.Trim(),
        Code = line.Code.Trim(),
        Name = line.Name.Trim(),
        DisplayColour = line.DisplayColour,
        SuggestedContainer = string.IsNullOrWhiteSpace(line.SuggestedContainer) ? null : line.SuggestedContainer.Trim(),
        _targetValue = target.Value,
        _targetUnit = target.Unit,
        _toleranceValue = tolerance?.Value,
    };
}
