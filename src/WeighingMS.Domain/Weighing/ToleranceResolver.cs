using WeighingMS.Domain.Common;

namespace WeighingMS.Domain.Weighing;

/// <summary>Decision TOL: the recipe tolerance if given, else the configured one, else blocked.</summary>
public static class ToleranceResolver
{
    /// <summary>
    /// Resolves the tolerance. <paramref name="configured"/> is the configured tolerance for the chemical on the
    /// selected scale class (or null); it is only consulted when the recipe gives none.
    /// </summary>
    public static ToleranceResolution Resolve(Quantity? recipeTolerance, Func<Quantity?> configured)
    {
        ArgumentNullException.ThrowIfNull(configured);
        if (recipeTolerance is { Value: > 0m })
        {
            return ToleranceResolution.Resolved(recipeTolerance, ToleranceSource.Recipe);
        }

        return configured() is { Value: > 0m } fromConfiguration
            ? ToleranceResolution.Resolved(fromConfiguration, ToleranceSource.Configured)
            : ToleranceResolution.Blocked;
    }
}
