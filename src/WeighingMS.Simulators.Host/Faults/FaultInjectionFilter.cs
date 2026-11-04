using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using WeighingMS.Simulators.Host.Common;
using WeighingMS.Simulators.Host.Seed;

namespace WeighingMS.Simulators.Host.Faults;

/// <summary>Action filter behind <see cref="FaultInjectionAttribute"/>.</summary>
public sealed class FaultInjectionFilter(
    string target,
    FaultRegistry faults,
    TimeProvider clock,
    IOptions<SimulatorOptions> options) : IAsyncActionFilter
{
    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context.ActionDescriptor.EndpointMetadata.OfType<FaultExemptAttribute>().Any())
        {
            await next();
            return;
        }

        var resolved = ResolveTarget(context);
        var mode = resolved is null ? null : faults.Get(resolved);
        var aborted = context.HttpContext.RequestAborted;
        switch (mode)
        {
            case FaultMode.Unavailable:
                context.Result = SimProblems.Create(
                    StatusCodes.Status503ServiceUnavailable,
                    FaultTargets.UnavailableCode(Area(resolved!)),
                    "Dummy unavailable",
                    $"'{resolved}' is unavailable (injected fault).");
                return;
            case FaultMode.Error500:
                context.Result = SimProblems.Create(
                    StatusCodes.Status500InternalServerError, "Simulator.InternalError", "Dummy internal error", $"'{resolved}' failed (injected fault).");
                return;
            case FaultMode.Timeout:
                await Task.Delay(options.Value.FaultTimeout, clock, aborted);
                context.Result = SimProblems.Create(
                    StatusCodes.Status504GatewayTimeout, "Simulator.Timeout", "Dummy timed out", $"'{resolved}' did not answer in time (injected fault).");
                return;
            case FaultMode.Slow:
                await Task.Delay(options.Value.SlowDelay, clock, aborted);
                await next();
                return;
            default:
                await next();
                return;
        }
    }

    private string? ResolveTarget(ActionExecutingContext context)
    {
        if (target != FaultTargets.ScaleFromRoute)
        {
            return target;
        }

        return context.RouteData.Values.TryGetValue("scaleId", out var id) && id is string scaleId
            ? FaultTargets.Scale(scaleId)
            : null;
    }

    private static string Area(string resolved) =>
        resolved.StartsWith(FaultTargets.ScalePrefix, StringComparison.Ordinal) ? FaultTargets.ScaleFromRoute : resolved;
}
