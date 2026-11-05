using WeighingMS.Api.Contracts.Devices;

namespace WeighingMS.Api.Contracts.Weighing;

/// <summary>
/// The active component's live reading from its own scale, classified on the server (WEIGH-READING). <c>class</c> is
/// <c>underweight | withinRange | targetAchieved | overweight</c>, or null when the tolerance is blocked or the reading
/// has no usable value. Quantities are in the scale's unit.
/// </summary>
public sealed record WeighingReadingResponse(
    int Sequence,
    string ScaleId,
    QuantityResponse? Net,
    bool Stable,
    string Validity,
    DateTimeOffset ReadAt,
    string? Class,
    QuantityResponse? Remaining,
    ToleranceBandResponse? Band,
    string ToleranceStatus,
    ConfirmationCheckResponse Confirmation);

/// <summary>Target, ± and the inclusive range, e.g. "TOLERANCE ±0.5 g (93.5 – 94.5)".</summary>
public sealed record ToleranceBandResponse(QuantityResponse Target, QuantityResponse PlusMinus, QuantityResponse Lower, QuantityResponse Upper);
