using WeighingMS.Domain.Batches;
using WeighingMS.Domain.Common;

namespace WeighingMS.Domain.Weighing;

/// <summary>
/// One recipe line inside a <see cref="WeighingSession"/>. Its scale and tolerance are fixed when the session starts
/// (assumption A1), so every reading and record of the batch is judged by the same rules.
/// </summary>
public sealed class ComponentWeighing
{
    private decimal _targetValue;
    private MassUnit _targetUnit;
    private MassUnit _scaleUnit;
    private decimal _scaleResolution;
    private decimal? _toleranceValue;
    private MassUnit? _toleranceUnit;

    private ComponentWeighing()
    {
        ChemicalId = string.Empty;
        Code = string.Empty;
        Name = string.Empty;
        ScaleId = string.Empty;
    }

    /// <summary>Identifier (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>Recipe sequence (1-based); identity within the session.</summary>
    public int Sequence { get; private set; }

    /// <summary>OrgaTEX chemical ID.</summary>
    public string ChemicalId { get; private set; }

    /// <summary>Chemical code.</summary>
    public string Code { get; private set; }

    /// <summary>Chemical name.</summary>
    public string Name { get; private set; }

    /// <summary>Suggested container from OrgaTEX, if any.</summary>
    public string? SuggestedContainer { get; private set; }

    /// <summary>The selected scale's class.</summary>
    public ScaleClass ScaleClass { get; private set; }

    /// <summary>The selected scale (automatic, FR-8).</summary>
    public string ScaleId { get; private set; }

    /// <summary>Where the tolerance came from, or null when blocked.</summary>
    public ToleranceSource? ToleranceSource { get; private set; }

    /// <summary>Pending, active or done.</summary>
    public ComponentState State { get; private set; }

    /// <summary>The required quantity.</summary>
    public Quantity Target => Quantity.Of(_targetValue, _targetUnit);

    /// <summary>The selected scale's unit and resolution, as read at session start.</summary>
    public ScaleProfile Scale => new(ScaleId, _scaleUnit, Quantity.Of(_scaleResolution, _scaleUnit));

    /// <summary>The applied tolerance, or blocked.</summary>
    public ToleranceResolution Tolerance =>
        _toleranceValue is { } value && _toleranceUnit is { } unit && ToleranceSource is { } source
            ? ToleranceResolution.Resolved(Quantity.Of(value, unit), source)
            : ToleranceResolution.Blocked;

    /// <summary>The band for this component in the scale's unit, or null when the tolerance is blocked.</summary>
    public ToleranceBand? Band => Tolerance.PlusMinus is { } plusMinus
        ? ToleranceBand.Create(Target.RoundTo(Scale.Resolution), plusMinus, _scaleUnit)
        : null;

    internal static ComponentWeighing Create(SnapshotComponent line, ScaleSelection selection, ScaleProfile scale, ToleranceResolution tolerance) => new()
    {
        Id = Guid.CreateVersion7(),
        Sequence = line.Sequence,
        ChemicalId = line.ChemicalId,
        Code = line.Code,
        Name = line.Name,
        SuggestedContainer = line.SuggestedContainer,
        ScaleClass = selection.ScaleClass,
        ScaleId = selection.ScaleId,
        State = ComponentState.Pending,
        _targetValue = line.Target.Value,
        _targetUnit = line.Target.Unit,
        _scaleUnit = scale.Unit,
        _scaleResolution = scale.Resolution.ConvertTo(scale.Unit).Value,
        _toleranceValue = tolerance.PlusMinus?.Value,
        _toleranceUnit = tolerance.PlusMinus?.Unit,
        ToleranceSource = tolerance.Source,
    };

    /// <summary>Classifies a net reading taken on this component's scale; null when the tolerance is blocked (A3).</summary>
    public Classification? Classify(Quantity net) =>
        Tolerance.PlusMinus is { } plusMinus ? ReadingClassifier.Classify(Target, plusMinus, net, Scale) : null;

    internal void Activate() => State = ComponentState.Active;

    internal void ReturnToPending() => State = ComponentState.Pending;
}
