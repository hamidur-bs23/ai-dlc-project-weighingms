using WeighingMS.Domain.Common;

namespace WeighingMS.Domain.Weighing;

/// <summary>
/// The tolerance that applies to a component, or <see cref="IsBlocked"/> when neither the recipe nor the configuration
/// gives one ("no tolerance defined"; there is no universal default).
/// </summary>
public sealed record ToleranceResolution
{
    private ToleranceResolution(Quantity? plusMinus, ToleranceSource? source)
    {
        PlusMinus = plusMinus;
        Source = source;
    }

    /// <summary>The ± tolerance, or null when blocked.</summary>
    public Quantity? PlusMinus { get; }

    /// <summary>Where it came from, or null when blocked.</summary>
    public ToleranceSource? Source { get; }

    /// <summary>True when the component cannot be weighed because no tolerance is defined.</summary>
    public bool IsBlocked => PlusMinus is null;

    /// <summary>No tolerance in the recipe or the configuration.</summary>
    public static ToleranceResolution Blocked { get; } = new(null, null);

    /// <summary>A tolerance from the given source.</summary>
    public static ToleranceResolution Resolved(Quantity plusMinus, ToleranceSource source)
    {
        ArgumentNullException.ThrowIfNull(plusMinus);
        if (plusMinus.Value <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(plusMinus), plusMinus, "A tolerance must be greater than zero.");
        }

        return new ToleranceResolution(plusMinus, source);
    }
}
