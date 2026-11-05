using WeighingMS.Domain.Common;

namespace WeighingMS.Domain.Weighing;

/// <summary>A classified reading, all quantities in the scale's unit.</summary>
public sealed record Classification(WeightClass Class, Quantity Net, Quantity Remaining, ToleranceBand Band)
{
    /// <summary>Within range or target achieved (the classes Confirm accepts).</summary>
    public bool IsConfirmable => Class is WeightClass.WithinRange or WeightClass.TargetAchieved;

    /// <summary>The surplus when Overweight (A16), else null.</summary>
    public Surplus? Surplus => Weighing.Surplus.Of(this);
}
