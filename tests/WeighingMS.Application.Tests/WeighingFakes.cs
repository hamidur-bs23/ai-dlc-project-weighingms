using ErrorOr;
using Microsoft.Extensions.Time.Testing;
using WeighingMS.Application.Batches;
using WeighingMS.Application.Devices;
using WeighingMS.Application.Weighing;
using WeighingMS.Domain.Audit;
using WeighingMS.Domain.Batches;
using WeighingMS.Domain.Common;
using WeighingMS.Domain.Weighing;

namespace WeighingMS.Application.Tests;

// Fakes for the weighing-session use cases (bolts 007–008): one per port.
internal sealed class FakeOrgatex : IOrgatexGateway
{
    public bool Down { get; set; }

    public decimal? SodaAshTolerance { get; set; } = 0.048m;

    public int RecipeCalls { get; private set; }

    public List<RecipeLine> Lines() =>
    [
        new(1, "DYE-1180", "DYE-1180", "Remazol Brill. Blue RSPL", "#1f4fa8", 1240.0m, "g", 6.0m, "C-09"),
        new(2, "DYE-2204", "DYE-2204", "Remazol Red RB", "#b8253a", 386.0m, "g", 2.0m, "C-11"),
        new(3, "DYE-4471", "DYE-4471", "Remazol Yellow RGB", "#d9a520", 94.0m, "g", 0.5m, "C-12"),
        new(4, "AUX-0101", "AUX-0101", "Sodium Sulphate", "#c9d2dc", 24.000m, "kg", 0.120m, "D-03"),
        new(5, "AUX-0107", "AUX-0107", "Soda Ash", "#e4eaf0", 9.600m, "kg", SodaAshTolerance, "D-05"),
    ];

    public Task<ErrorOr<IReadOnlyList<OrgatexBatch>>> GetBatchesAsync(CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<ErrorOr<OrgatexBatch>> GetBatchAsync(string batchId, CancellationToken cancellationToken) =>
        Task.FromResult<ErrorOr<OrgatexBatch>>(Down
            ? OrgatexErrors.Unavailable
            : new OrgatexBatch(batchId, "SO-9931", "JET-04", "100% CTN knit", 480m, "Teal 4C", 5, false));

    public Task<ErrorOr<OrgatexRecipe>> GetRecipeAsync(string batchId, CancellationToken cancellationToken)
    {
        RecipeCalls++;
        return Task.FromResult<ErrorOr<OrgatexRecipe>>(Down ? OrgatexErrors.Unavailable : new OrgatexRecipe(batchId, "Teal 4C", false, Lines()));
    }
}

internal sealed class FakeSnapshots : IRecipeSnapshotStore
{
    public List<RecipeSnapshot> Stored { get; } = [];

    public Task<RecipeSnapshot?> FindAsync(string batchId, CancellationToken cancellationToken) =>
        Task.FromResult(Stored.FirstOrDefault(s => s.BatchId == batchId));

    public Task<IReadOnlySet<string>> WithSnapshotAsync(IReadOnlyCollection<string> batchIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlySet<string>>(Stored.Select(s => s.BatchId).Where(batchIds.Contains).ToHashSet());

    public Task<RecipeSnapshot> AddOrGetExistingAsync(RecipeSnapshot snapshot, CancellationToken cancellationToken)
    {
        Stored.Add(snapshot);
        return Task.FromResult(snapshot);
    }
}

internal sealed class FakeSessions : IWeighingSessionRepository
{
    public List<WeighingSession> Stored { get; } = [];

    public WeighingSession? RaceWinner { get; set; }

    public bool Conflict { get; set; }

    public int Saves { get; set; }

    public Task<WeighingSession?> FindAsync(string batchId, CancellationToken cancellationToken) =>
        Task.FromResult(Stored.FirstOrDefault(s => s.BatchId == batchId));

    public Task<(WeighingSession Session, bool Started)> AddOrGetExistingAsync(WeighingSession session, CancellationToken cancellationToken)
    {
        if (RaceWinner is { } winner)
        {
            Stored.Add(winner);
            return Task.FromResult((winner, false));
        }

        Stored.Add(session);
        return Task.FromResult((session, true));
    }

    public Task<ErrorOr<Success>> SaveAsync(CancellationToken cancellationToken)
    {
        Saves++;
        return Task.FromResult<ErrorOr<Success>>(Conflict ? WeighingErrors.ConcurrencyConflict : Result.Success);
    }
}

internal sealed class FakeTolerances : IToleranceCatalogue
{
    public Dictionary<(string, string), Quantity> Entries { get; } = [];

    public Quantity? Find(string chemicalId, string scaleClass) => Entries.GetValueOrDefault((chemicalId, scaleClass));
}

internal sealed class FakeSelection : IScaleSelectionSource
{
    public ScaleSelectionPolicy Policy { get; set; } = ScaleSelectionPolicy.Create(3.0m, "scale-3kg", "scale-32kg").Value;

    public Error? Error { get; set; }

    public ErrorOr<ScaleSelectionPolicy> Current() => Error is { } e ? e : Policy;
}

internal sealed class FakeScales : IScaleDevice
{
    public bool Down { get; set; }

    public int ListCalls { get; private set; }

    public string? LastReadScale { get; private set; }

    public RawScaleReading? Reading { get; set; }

    public Task<ErrorOr<IReadOnlyList<ScaleInfo>>> ListAsync(CancellationToken cancellationToken)
    {
        ListCalls++;
        IReadOnlyList<ScaleInfo> list =
        [
            new("scale-3kg", "3 kg precision", "precision", new DeviceQuantity(3200m, "g"), new DeviceQuantity(0.1m, "g")),
            new("scale-32kg", "32 kg floor", "heavyDuty", new DeviceQuantity(32m, "kg"), new DeviceQuantity(0.001m, "kg")),
        ];
        return Task.FromResult<ErrorOr<IReadOnlyList<ScaleInfo>>>(Down ? DeviceErrors.ScaleUnavailable("all") : list.ToList());
    }

    public Task<ErrorOr<DeviceStatus>> GetStatusAsync(string scaleId, CancellationToken cancellationToken) =>
        Task.FromResult<ErrorOr<DeviceStatus>>(DeviceStatus.Ready);

    public Task<ErrorOr<RawScaleReading>> ReadAsync(string scaleId, CancellationToken cancellationToken)
    {
        LastReadScale = scaleId;
        return Task.FromResult<ErrorOr<RawScaleReading>>(Down || Reading is null ? DeviceErrors.ScaleUnavailable(scaleId) : Reading);
    }

    public decimal TareValue { get; set; } = 214.0m;

    public int TareCalls { get; private set; }

    public Task<ErrorOr<TareResult>> TareAsync(string scaleId, CancellationToken cancellationToken)
    {
        TareCalls++;
        var unit = scaleId == "scale-3kg" ? "g" : "kg";
        return Task.FromResult<ErrorOr<TareResult>>(Down ? DeviceErrors.ScaleUnavailable(scaleId) : new TareResult(scaleId, new DeviceQuantity(TareValue, unit), DateTimeOffset.UnixEpoch));
    }
}

internal sealed class NoScanner : IScanner
{
    public Task<ErrorOr<IReadOnlyList<ScanEvent>>> GetScansAfterAsync(long after, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<ErrorOr<DeviceStatus>> GetStatusAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
}
