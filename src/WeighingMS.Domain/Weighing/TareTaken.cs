using WeighingMS.Domain.Common;

namespace WeighingMS.Domain.Weighing;

/// <summary>
/// A tare taken in the current attempt (FR-7): the value the scale reported (any value, including 0, A7), on which
/// scale and when. It only counts on the component's own scale and after that scale last came online (story 005).
/// </summary>
public sealed record TareTaken(string ScaleId, Quantity Tare, DateTimeOffset TakenAt);
