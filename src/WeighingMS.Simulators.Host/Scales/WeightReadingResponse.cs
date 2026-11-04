namespace WeighingMS.Simulators.Host.Scales;

/// <summary>One net reading, rounded to the scale resolution (training contract).</summary>
public sealed record WeightReadingResponse(
    string ScaleId,
    decimal Net,
    string Unit,
    bool Stable,
    bool OverCapacity,
    DateTimeOffset Timestamp);
