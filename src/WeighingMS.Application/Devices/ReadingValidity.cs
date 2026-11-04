namespace WeighingMS.Application.Devices;

/// <summary>Whether a scale reading can be used (FR-23). Only <see cref="Valid"/> readings may ever enable Confirm.</summary>
public enum ReadingValidity
{
    /// <summary>A usable reading.</summary>
    Valid,

    /// <summary>Above the scale's capacity ("Over capacity").</summary>
    OverCapacity,

    /// <summary>Below zero beyond the scale's zero band.</summary>
    BelowZero,

    /// <summary>Missing or not a number.</summary>
    Malformed,
}
