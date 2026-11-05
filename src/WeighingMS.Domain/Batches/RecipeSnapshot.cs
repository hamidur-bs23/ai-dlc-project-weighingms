using ErrorOr;
using WeighingMS.Domain.Common;

namespace WeighingMS.Domain.Batches;

/// <summary>
/// The recipe of a batch, copied from OrgaTEX when the batch is first opened (decision RECIPE-SNAPSHOT) and never
/// changed afterwards: weighing continues on this copy even if OrgaTEX later goes offline (FR-3, FR-24a), and the
/// job → recipe → chemical link is kept for every record.
/// </summary>
public sealed class RecipeSnapshot
{
    private readonly List<SnapshotComponent> _components = [];

    private RecipeSnapshot()
    {
        BatchId = string.Empty;
        OrderId = string.Empty;
        Machine = string.Empty;
        Fabric = string.Empty;
        Shade = string.Empty;
    }

    /// <summary>Identifier (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>OrgaTEX batch ID (natural key, unique).</summary>
    public string BatchId { get; private set; }

    /// <summary>Order.</summary>
    public string OrderId { get; private set; }

    /// <summary>Dyeing machine.</summary>
    public string Machine { get; private set; }

    /// <summary>Fabric description.</summary>
    public string Fabric { get; private set; }

    /// <summary>Batch weight in kg.</summary>
    public decimal BatchWeightKg { get; private set; }

    /// <summary>Shade.</summary>
    public string Shade { get; private set; }

    /// <summary>True for invented lab recipes (SEED-01).</summary>
    public bool TrainingOnly { get; private set; }

    /// <summary>When the snapshot was taken (UTC).</summary>
    public DateTimeOffset LoadedAt { get; private set; }

    /// <summary>The recipe lines in weighing sequence.</summary>
    public IReadOnlyList<SnapshotComponent> Components => [.. _components.OrderBy(c => c.Sequence)];

    /// <summary>
    /// Validates the OrgaTEX recipe and creates the snapshot. Rejects: no lines, duplicate sequences, unknown
    /// units, targets ≤ 0 and non-positive tolerances (<see cref="BatchErrors.InvalidRecipe"/>).
    /// </summary>
    public static ErrorOr<RecipeSnapshot> Create(BatchHeader header, IReadOnlyCollection<RecipeLine> lines, DateTimeOffset loadedAt)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count == 0)
        {
            return BatchErrors.InvalidRecipe(header.BatchId, "it has no components");
        }

        if (lines.Select(l => l.Sequence).Distinct().Count() != lines.Count)
        {
            return BatchErrors.InvalidRecipe(header.BatchId, "sequence numbers are not unique");
        }

        var snapshot = new RecipeSnapshot
        {
            Id = Guid.CreateVersion7(),
            BatchId = header.BatchId.Trim(),
            OrderId = header.OrderId,
            Machine = header.Machine,
            Fabric = header.Fabric,
            BatchWeightKg = header.BatchWeightKg,
            Shade = header.Shade,
            TrainingOnly = header.TrainingOnly,
            LoadedAt = loadedAt.ToUniversalTime(),
        };

        foreach (var line in lines)
        {
            var target = Quantity.CreatePositive(line.Target, line.Unit);
            if (target.IsError)
            {
                return BatchErrors.InvalidRecipe(header.BatchId, $"component {line.Sequence} ({line.Code}): {target.FirstError.Description}");
            }

            Quantity? tolerance = null;
            if (line.Tolerance is { } t)
            {
                var parsed = Quantity.CreatePositive(t, line.Unit);
                if (parsed.IsError)
                {
                    return BatchErrors.InvalidRecipe(header.BatchId, $"component {line.Sequence} ({line.Code}): tolerance must be greater than zero");
                }

                tolerance = parsed.Value;
            }

            snapshot._components.Add(SnapshotComponent.From(line, target.Value, tolerance));
        }

        return snapshot;
    }
}
