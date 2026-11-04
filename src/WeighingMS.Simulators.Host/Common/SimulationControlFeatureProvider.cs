using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace WeighingMS.Simulators.Host.Common;

/// <summary>
/// Removes every <see cref="SimulationControlAttribute"/> controller when simulation control is disabled,
/// so <c>/_sim/*</c> routes do not exist outside Development and Test.
/// </summary>
public sealed class SimulationControlFeatureProvider(bool simulationControlEnabled)
    : IApplicationFeatureProvider<ControllerFeature>
{
    /// <inheritdoc />
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        if (simulationControlEnabled)
        {
            return;
        }

        var simulationControllers = feature.Controllers
            .Where(c => c.GetCustomAttribute<SimulationControlAttribute>() is not null)
            .ToList();
        foreach (var controller in simulationControllers)
        {
            feature.Controllers.Remove(controller);
        }
    }
}
