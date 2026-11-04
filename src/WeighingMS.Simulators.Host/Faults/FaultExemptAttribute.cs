namespace WeighingMS.Simulators.Host.Faults;

/// <summary>
/// Excludes an action from fault injection. Used for status endpoints, which report OFFLINE instead of failing.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class FaultExemptAttribute : Attribute;
