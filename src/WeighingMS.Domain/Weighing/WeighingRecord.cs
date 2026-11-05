using WeighingMS.Domain.Common;

namespace WeighingMS.Domain.Weighing;

/// <summary>
/// The immutable result of a confirmed weighing (FR-19, workbook STEP 25): job → recipe → chemical, required and
/// actual quantity, class, tolerance and source, scale, lot, container, tare, operator and time. One per component;
/// never changed or deleted (A9).
/// </summary>
public sealed class WeighingRecord
{
    private decimal _requiredValue;
    private MassUnit _requiredUnit;
    private decimal _actualValue;
    private MassUnit _actualUnit;
    private decimal _toleranceValue;
    private MassUnit _toleranceUnit;
    private decimal _tareValue;
    private MassUnit _tareUnit;

    private WeighingRecord()
    {
        BatchId = string.Empty;
        ChemicalId = string.Empty;
        ChemicalName = string.Empty;
        ScaleId = string.Empty;
        Lot = string.Empty;
        Container = string.Empty;
        OperatorBadgeId = string.Empty;
    }

    /// <summary>Identifier (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>The session the record belongs to.</summary>
    public Guid SessionId { get; private set; }

    /// <summary>Batch (job).</summary>
    public string BatchId { get; private set; }

    /// <summary>Recipe sequence of the component.</summary>
    public int Sequence { get; private set; }

    /// <summary>OrgaTEX chemical ID.</summary>
    public string ChemicalId { get; private set; }

    /// <summary>Chemical name.</summary>
    public string ChemicalName { get; private set; }

    /// <summary>The result class (Within range or Target achieved; overrides in bolt 009).</summary>
    public WeightClass Class { get; private set; }

    /// <summary>Where the applied tolerance came from.</summary>
    public ToleranceSource ToleranceSource { get; private set; }

    /// <summary>The scale used.</summary>
    public string ScaleId { get; private set; }

    /// <summary>The scale's class.</summary>
    public ScaleClass ScaleClass { get; private set; }

    /// <summary>Dyestuff lot.</summary>
    public string Lot { get; private set; }

    /// <summary>Container ID.</summary>
    public string Container { get; private set; }

    /// <summary>Badge of the operator who confirmed.</summary>
    public string OperatorBadgeId { get; private set; }

    /// <summary>When it was confirmed (UTC).</summary>
    public DateTimeOffset ConfirmedAt { get; private set; }

    /// <summary>The required quantity (recipe target).</summary>
    public Quantity Required => Quantity.Of(_requiredValue, _requiredUnit);

    /// <summary>The actual quantity: the server's reading at confirm time, at the scale resolution.</summary>
    public Quantity Actual => Quantity.Of(_actualValue, _actualUnit);

    /// <summary>The ± tolerance applied.</summary>
    public Quantity Tolerance => Quantity.Of(_toleranceValue, _toleranceUnit);

    /// <summary>The tare the scale reported for this attempt.</summary>
    public Quantity Tare => Quantity.Of(_tareValue, _tareUnit);

    internal static WeighingRecord Create(Guid sessionId, string batchId, ComponentWeighing component, Classification classification, WeighingAttempt attempt, string operatorBadgeId, DateTimeOffset now)
    {
        var tolerance = component.Tolerance;
        return new WeighingRecord
        {
            Id = Guid.CreateVersion7(),
            SessionId = sessionId,
            BatchId = batchId,
            Sequence = component.Sequence,
            ChemicalId = component.ChemicalId,
            ChemicalName = component.Name,
            Class = classification.Class,
            ToleranceSource = tolerance.Source!.Value,
            ScaleId = component.ScaleId,
            ScaleClass = component.ScaleClass,
            Lot = attempt.Lot!,
            Container = attempt.Container!,
            OperatorBadgeId = operatorBadgeId,
            ConfirmedAt = now.ToUniversalTime(),
            _requiredValue = component.Target.Value,
            _requiredUnit = component.Target.Unit,
            _actualValue = classification.Net.Value,
            _actualUnit = classification.Net.Unit,
            _toleranceValue = tolerance.PlusMinus!.Value,
            _toleranceUnit = tolerance.PlusMinus.Unit,
            _tareValue = attempt.Tare!.Tare.Value,
            _tareUnit = attempt.Tare.Tare.Unit,
        };
    }
}
