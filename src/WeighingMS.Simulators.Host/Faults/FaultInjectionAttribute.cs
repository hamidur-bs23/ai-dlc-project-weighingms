using Microsoft.AspNetCore.Mvc;

namespace WeighingMS.Simulators.Host.Faults;

/// <summary>
/// Applies the active fault of <paramref name="target"/> to every action of a training-contract controller.
/// Use <see cref="FaultTargets.ScaleFromRoute"/> to resolve the scale from the <c>scaleId</c> route value.
/// Actions marked <see cref="FaultExemptAttribute"/> (status endpoints) are not affected.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class FaultInjectionAttribute : TypeFilterAttribute
{
    /// <summary>Creates the filter for a target.</summary>
    public FaultInjectionAttribute(string target)
        : base(typeof(FaultInjectionFilter))
    {
        Target = target;
        Arguments = [target];
    }

    /// <summary>The faulted dummy.</summary>
    public string Target { get; }
}
