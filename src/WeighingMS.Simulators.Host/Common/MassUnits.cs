namespace WeighingMS.Simulators.Host.Common;

/// <summary>Conversion between the two mass units used by the training data (g and kg).</summary>
public static class MassUnits
{
    /// <summary>Grams.</summary>
    public const string Gram = "g";

    /// <summary>Kilograms.</summary>
    public const string Kilogram = "kg";

    /// <summary>True when the unit is g or kg.</summary>
    public static bool IsKnown(string? unit) => unit is Gram or Kilogram;

    /// <summary>Converts a quantity between g and kg using exact decimal arithmetic.</summary>
    public static decimal Convert(decimal value, string fromUnit, string toUnit)
    {
        if (!IsKnown(fromUnit) || !IsKnown(toUnit))
        {
            throw new ArgumentException($"Unknown mass unit '{fromUnit}' or '{toUnit}'.", nameof(fromUnit));
        }

        if (fromUnit == toUnit)
        {
            return value;
        }

        return fromUnit == Gram ? value / 1000m : value * 1000m;
    }

    /// <summary>Rounds a value to a resolution step (for example 0.1 or 0.001), midpoints away from zero.</summary>
    public static decimal RoundToResolution(decimal value, decimal resolution)
    {
        if (resolution <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(resolution), "Resolution must be positive.");
        }

        return Math.Round(value / resolution, MidpointRounding.AwayFromZero) * resolution;
    }
}
