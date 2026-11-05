using WeighingMS.Application.Devices;
using WeighingMS.Domain.Weighing;

namespace WeighingMS.Application.Weighing;

/// <summary>
/// The live reading of the active component, from its own scale (decision WEIGH-READING), with the confirm check the
/// screen uses to enable F1 (the same rule the server applies on confirm). <see cref="Classification"/> is null when the
/// tolerance is blocked (A3) or the reading has no usable value (malformed, over capacity).
/// </summary>
public sealed record CurrentReading(ComponentWeighing Component, ScaleReading Reading, ConfirmationCheck Check)
{
    /// <summary>The reading's class, remaining and band, if it could be classified.</summary>
    public Classification? Classification => Check.Classification;
}
