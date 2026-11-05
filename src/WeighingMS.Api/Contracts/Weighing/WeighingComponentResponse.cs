using WeighingMS.Api.Contracts.Devices;

namespace WeighingMS.Api.Contracts.Weighing;

/// <summary>One component of the session with its fixed scale and tolerance.</summary>
public sealed record WeighingComponentResponse(
    int Sequence,
    string ChemicalId,
    string Code,
    string Name,
    QuantityResponse Target,
    string State,
    ComponentScaleResponse Scale,
    ComponentToleranceResponse Tolerance,
    string? SuggestedContainer);

/// <summary>The automatically selected scale (FR-8).</summary>
public sealed record ComponentScaleResponse(string ScaleId, string ScaleClass, string Unit, QuantityResponse Resolution, bool Automatic);

/// <summary>
/// The applied tolerance: <c>status</c> is <c>resolved</c> (with source, ± and the inclusive band) or <c>blocked</c>
/// ("no tolerance defined", all other fields null).
/// </summary>
public sealed record ComponentToleranceResponse(
    string Status,
    string? Source,
    QuantityResponse? PlusMinus,
    QuantityResponse? Lower,
    QuantityResponse? Upper);
