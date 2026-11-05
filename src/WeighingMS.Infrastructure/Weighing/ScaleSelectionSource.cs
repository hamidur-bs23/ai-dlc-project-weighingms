using ErrorOr;
using Microsoft.Extensions.Options;
using WeighingMS.Application.Weighing;
using WeighingMS.Domain.Weighing;

namespace WeighingMS.Infrastructure.Weighing;

/// <summary><see cref="IScaleSelectionSource"/> over <see cref="IOptionsMonitor{TOptions}"/>: reloads without a restart.</summary>
public sealed class ScaleSelectionSource(IOptionsMonitor<ScaleSelectionOptions> options) : IScaleSelectionSource
{
    /// <inheritdoc />
    public ErrorOr<ScaleSelectionPolicy> Current()
    {
        var o = options.CurrentValue;
        return ScaleSelectionPolicy.Create(o.ThresholdKg, o.Assignments.Precision, o.Assignments.HeavyDuty);
    }
}
