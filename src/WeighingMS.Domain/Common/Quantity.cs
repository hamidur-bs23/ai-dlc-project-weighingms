using ErrorOr;

namespace WeighingMS.Domain.Common;

/// <summary>
/// A mass: fixed-point <see cref="decimal"/> value with its unit (coding-standards: never float/double for
/// quantities). Comparisons across units go through <see cref="InKilograms"/>.
/// </summary>
public sealed record Quantity
{
    private Quantity(decimal value, MassUnit unit)
    {
        Value = value;
        Unit = unit;
    }

    /// <summary>The amount.</summary>
    public decimal Value { get; }

    /// <summary>The unit.</summary>
    public MassUnit Unit { get; }

    /// <summary>The unit symbol: <c>g</c> or <c>kg</c>.</summary>
    public string Symbol => Unit == MassUnit.Gram ? "g" : "kg";

    /// <summary>The amount in kilograms (exact decimal conversion).</summary>
    public decimal InKilograms => Unit == MassUnit.Gram ? Value / 1000m : Value;

    /// <summary>Creates a quantity from a value and a unit symbol (<c>g</c> or <c>kg</c>).</summary>
    public static ErrorOr<Quantity> Create(decimal value, string? unitSymbol) =>
        ParseUnit(unitSymbol) is { } unit
            ? new Quantity(value, unit)
            : Error.Validation("Quantity.UnknownUnit", $"Unknown mass unit '{unitSymbol}'.");

    /// <summary>Creates a strictly positive quantity.</summary>
    public static ErrorOr<Quantity> CreatePositive(decimal value, string? unitSymbol)
    {
        if (value <= 0m)
        {
            return Error.Validation("Quantity.NotPositive", $"Quantity must be greater than zero (was {value}).");
        }

        return Create(value, unitSymbol);
    }

    /// <summary>The unit for a symbol, or null if unknown.</summary>
    public static MassUnit? ParseUnit(string? symbol) => symbol?.Trim() switch
    {
        "g" => MassUnit.Gram,
        "kg" => MassUnit.Kilogram,
        _ => null,
    };

    /// <inheritdoc />
    public override string ToString() => $"{Value} {Symbol}";
}
