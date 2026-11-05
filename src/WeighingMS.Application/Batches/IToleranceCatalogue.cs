using WeighingMS.Domain.Common;

namespace WeighingMS.Application.Batches;

/// <summary>Configured tolerances (TOL fallback); reloads without restart.</summary>
public interface IToleranceCatalogue
{
    /// <summary>The configured ± tolerance for a chemical on a scale class, or null when none is configured.</summary>
    Quantity? Find(string chemicalId, string scaleClass);
}
