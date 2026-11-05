namespace WeighingMS.Domain.Weighing;

/// <summary>The result of <see cref="ConfirmationRules.Check"/>: what is missing, and the reading's classification.</summary>
public sealed record ConfirmationCheck(IReadOnlyList<ConfirmationGap> Missing, Classification? Classification)
{
    /// <summary>True when nothing is missing.</summary>
    public bool CanConfirm => Missing.Count == 0;
}
