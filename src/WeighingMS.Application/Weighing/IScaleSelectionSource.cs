using ErrorOr;
using WeighingMS.Domain.Weighing;

namespace WeighingMS.Application.Weighing;

/// <summary>The current scale-selection policy from configuration; a change applies to the next session (A1).</summary>
public interface IScaleSelectionSource
{
    /// <summary>The policy as configured now.</summary>
    ErrorOr<ScaleSelectionPolicy> Current();
}
