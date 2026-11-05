using WeighingMS.Domain.Common;

namespace WeighingMS.Domain.Weighing;

/// <summary>
/// The inclusive band <c>[target − tol, target + tol]</c> in one unit (FR-10): "TOLERANCE ±0.5 g (93.5 – 94.5)".
/// </summary>
public sealed record ToleranceBand
{
    private ToleranceBand(Quantity target, Quantity plusMinus)
    {
        Target = target;
        PlusMinus = plusMinus;
        Lower = Quantity.Of(target.Value - plusMinus.Value, target.Unit);
        Upper = Quantity.Of(target.Value + plusMinus.Value, target.Unit);
    }

    /// <summary>The target.</summary>
    public Quantity Target { get; }

    /// <summary>The ± tolerance, in the target's unit.</summary>
    public Quantity PlusMinus { get; }

    /// <summary>Lowest acceptable quantity (inclusive).</summary>
    public Quantity Lower { get; }

    /// <summary>Highest acceptable quantity (inclusive).</summary>
    public Quantity Upper { get; }

    /// <summary>The band for a target and tolerance, both expressed in <paramref name="unit"/>.</summary>
    public static ToleranceBand Create(Quantity target, Quantity plusMinus, MassUnit unit)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(plusMinus);
        return new ToleranceBand(target.ConvertTo(unit), plusMinus.ConvertTo(unit));
    }
}
