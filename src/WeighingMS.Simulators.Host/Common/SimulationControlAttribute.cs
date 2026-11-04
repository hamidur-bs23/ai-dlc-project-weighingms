namespace WeighingMS.Simulators.Host.Common;

/// <summary>
/// Marks a controller as simulation control (<c>/_sim/*</c>). These controllers exist only in the
/// Development and Test environments and are never part of the training contracts (api-conventions).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SimulationControlAttribute : Attribute;
