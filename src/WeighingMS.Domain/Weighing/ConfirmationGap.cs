namespace WeighingMS.Domain.Weighing;

/// <summary>A condition for confirming that isn't met (FR-13). The same list drives the button and the server.</summary>
public enum ConfirmationGap
{
    /// <summary>No tolerance defined for the component (blocked, A3).</summary>
    Tolerance,

    /// <summary>No lot captured.</summary>
    Lot,

    /// <summary>No container captured.</summary>
    Container,

    /// <summary>No tare in this attempt on this scale.</summary>
    Tare,

    /// <summary>The scale came online after the tare: tare again.</summary>
    TareStale,

    /// <summary>The reading has no usable value or is not valid (below zero, over capacity, malformed).</summary>
    Reading,

    /// <summary>The reading is not stable yet.</summary>
    Stable,

    /// <summary>The reading is Underweight or Overweight.</summary>
    InTolerance,
}
