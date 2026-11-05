using Microsoft.Extensions.Options;
using WeighingMS.Application.Batches;
using WeighingMS.Domain.Common;

namespace WeighingMS.Infrastructure.Batches;

/// <summary>
/// <see cref="IToleranceCatalogue"/> over <see cref="IOptionsMonitor{TOptions}"/>: a configuration change applies to the
/// next lookup without a restart. No entry → null (no universal default).
/// </summary>
public sealed class ToleranceCatalogue(IOptionsMonitor<ToleranceSettings> settings) : IToleranceCatalogue
{
    /// <inheritdoc />
    public Quantity? Find(string chemicalId, string scaleClass)
    {
        var entry = settings.CurrentValue.Entries.FirstOrDefault(e =>
            string.Equals(e.ChemicalId, chemicalId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(e.ScaleClass, scaleClass, StringComparison.Ordinal));
        if (entry is null)
        {
            return null;
        }

        var tolerance = Quantity.CreatePositive(entry.PlusMinus, entry.Unit);
        return tolerance.IsError ? null : tolerance.Value;
    }
}
