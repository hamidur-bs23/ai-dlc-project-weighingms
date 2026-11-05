using WeighingMS.Domain.Common;

namespace WeighingMS.Domain.Weighing;

/// <summary>
/// A scale reading as the domain needs it: the net (null when the device gave no usable value), whether it is stable,
/// whether it is valid for confirming (not below zero, over capacity or malformed, A10), and when the scale last came
/// online (a tare taken before that is stale).
/// </summary>
public sealed record MeasuredReading(Quantity? Net, bool Stable, bool Valid, DateTimeOffset? OnlineSince);
