using WeighingMS.Api.Contracts.Devices;

namespace WeighingMS.Api.Contracts.Weighing;

/// <summary>
/// An immutable weighing record (FR-19): the STEP 25 transaction (actual weight, operator, component, lot, container,
/// tare, scale, timestamp) plus required quantity, class and the tolerance applied with its source.
/// </summary>
public sealed record WeighingRecordResponse(
    Guid RecordId,
    string BatchId,
    int Sequence,
    string ChemicalId,
    string ChemicalName,
    QuantityResponse Required,
    QuantityResponse Actual,
    string Class,
    AppliedToleranceResponse Tolerance,
    string ScaleId,
    string ScaleClass,
    string Lot,
    string Container,
    QuantityResponse Tare,
    string Operator,
    DateTimeOffset ConfirmedAt);

/// <summary>The ± tolerance a record was judged by, and where it came from.</summary>
public sealed record AppliedToleranceResponse(QuantityResponse PlusMinus, string Source);

/// <summary>A successful confirm: the record, and the session with the next component active.</summary>
public sealed record ConfirmationResponse(WeighingRecordResponse Record, WeighingSessionResponse Session);

/// <summary>The current attempt of the active component.</summary>
public sealed record AttemptResponse(string? Lot, string? Container, QuantityResponse? Tare, DateTimeOffset? TaredAt);

/// <summary>A done component's result, for the rail ("✓ 94.2 g").</summary>
public sealed record ComponentRecordResponse(Guid RecordId, QuantityResponse Actual, string Class, string Operator, DateTimeOffset ConfirmedAt);

/// <summary>Whether Confirm (F1) may be pressed now, and what is missing (the same rule the server applies).</summary>
public sealed record ConfirmationCheckResponse(bool CanConfirm, IReadOnlyList<string> Missing);
