namespace WeighingMS.Simulators.Host.Scales;

/// <summary>Weighing scenarios the dummy scale can play (PDF §9 simulation scenarios).</summary>
public enum ScaleScenario
{
    /// <summary>Weight rises gradually and lands on the target.</summary>
    Rising,

    /// <summary>Weight rises and stays below range (target − 2 × tolerance).</summary>
    BelowRange,

    /// <summary>Weight rises and settles inside range (target + 0.4 × tolerance).</summary>
    EnterRange,

    /// <summary>Weight rises past the upper bound (target + 4.8 × tolerance).</summary>
    Overshoot,

    /// <summary>Weight holds its current value.</summary>
    Stable,
}
