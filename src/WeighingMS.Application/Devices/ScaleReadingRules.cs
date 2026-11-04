namespace WeighingMS.Application.Devices;

/// <summary>Classifies a raw reading (FR-23). Pure and deterministic; the only place validity is decided.</summary>
public static class ScaleReadingRules
{
    /// <summary>Malformed, then over capacity, then below zero beyond the band; otherwise valid.</summary>
    public static ReadingValidity Classify(RawScaleReading raw, decimal zeroBand)
    {
        ArgumentNullException.ThrowIfNull(raw);
        if (raw.Net is null || string.IsNullOrWhiteSpace(raw.Unit))
        {
            return ReadingValidity.Malformed;
        }

        if (raw.OverCapacity)
        {
            return ReadingValidity.OverCapacity;
        }

        return raw.Net.Value < -Math.Abs(zeroBand) ? ReadingValidity.BelowZero : ReadingValidity.Valid;
    }
}
