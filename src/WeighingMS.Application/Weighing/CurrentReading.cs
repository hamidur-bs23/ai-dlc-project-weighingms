using WeighingMS.Application.Devices;
using WeighingMS.Domain.Weighing;

namespace WeighingMS.Application.Weighing;

/// <summary>
/// The live reading of the active component, from its own scale (decision WEIGH-READING). <see cref="Classification"/>
/// is null when the tolerance is blocked (A3) or the reading has no usable value (malformed, over capacity).
/// </summary>
public sealed record CurrentReading(ComponentWeighing Component, ScaleReading Reading, Classification? Classification);
