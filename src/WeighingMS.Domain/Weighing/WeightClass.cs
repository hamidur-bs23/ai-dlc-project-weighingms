namespace WeighingMS.Domain.Weighing;

/// <summary>The four classes of a reading (FR-12, PDF §7). Target achieved is distinct from Within range.</summary>
public enum WeightClass
{
    /// <summary>Below the band: keep dispensing.</summary>
    Underweight,

    /// <summary>In the band but not exactly on target: confirmable.</summary>
    WithinRange,

    /// <summary>Exactly on target at the scale's resolution: confirmable.</summary>
    TargetAchieved,

    /// <summary>Above the band: not confirmable without an authorised exception.</summary>
    Overweight,
}
