using WeighingMS.Domain.Common;

namespace WeighingMS.Domain.Weighing;

/// <summary>
/// What the domain needs to know about a scale: the unit it reports in and its resolution (decision SCALE-PROFILE:
/// read once at session start and kept with the session).
/// </summary>
public sealed record ScaleProfile(string ScaleId, MassUnit Unit, Quantity Resolution);
