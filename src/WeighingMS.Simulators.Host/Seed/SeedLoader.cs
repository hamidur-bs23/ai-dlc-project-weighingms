using System.Text.Json;

namespace WeighingMS.Simulators.Host.Seed;

/// <summary>Loads and validates the training seed. An invalid seed stops start-up.</summary>
public static class SeedLoader
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Reads the seed file and validates it.</summary>
    /// <exception cref="InvalidOperationException">The seed is missing or invalid.</exception>
    public static SimulatorSeed Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Simulator seed file '{path}' was not found.");
        }

        var seed = JsonSerializer.Deserialize<SimulatorSeed>(File.ReadAllText(path), _jsonOptions)
            ?? throw new InvalidOperationException($"Simulator seed file '{path}' is empty.");
        Validate(seed);
        return seed;
    }

    /// <summary>Validates seed invariants (unique ids, positive quantities, known units).</summary>
    /// <exception cref="InvalidOperationException">An invariant is violated.</exception>
    public static void Validate(SimulatorSeed seed)
    {
        ArgumentNullException.ThrowIfNull(seed);
        var errors = new List<string>();

        if (seed.Batches is null || seed.Batches.Count == 0)
        {
            errors.Add("seed has no batches");
        }
        else
        {
            foreach (var duplicate in seed.Batches.GroupBy(b => b.BatchId).Where(g => g.Count() > 1))
            {
                errors.Add($"duplicate batch '{duplicate.Key}'");
            }

            foreach (var batch in seed.Batches)
            {
                ValidateBatch(batch, errors);
            }
        }

        if (seed.Scales is null || seed.Scales.Count == 0)
        {
            errors.Add("seed has no scales");
        }
        else
        {
            foreach (var scale in seed.Scales.Where(s => s.Resolution <= 0m || s.Capacity <= 0m || !Common.MassUnits.IsKnown(s.Unit)))
            {
                errors.Add($"scale '{scale.ScaleId}' needs a known unit and positive capacity and resolution");
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Invalid simulator seed: " + string.Join("; ", errors));
        }
    }

    private static void ValidateBatch(SeedBatch batch, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(batch.BatchId))
        {
            errors.Add("a batch has no batchId");
            return;
        }

        if (batch.Components is null || batch.Components.Count == 0)
        {
            errors.Add($"batch '{batch.BatchId}' has no components");
            return;
        }

        foreach (var c in batch.Components)
        {
            if (c.TargetQuantity <= 0m || c.Tolerance <= 0m || !Common.MassUnits.IsKnown(c.Unit))
            {
                errors.Add($"batch '{batch.BatchId}' component {c.Sequence} needs a known unit and positive target and tolerance");
            }
        }

        if (batch.Components.Select(c => c.Sequence).Distinct().Count() != batch.Components.Count)
        {
            errors.Add($"batch '{batch.BatchId}' has duplicate sequence numbers");
        }
    }
}
