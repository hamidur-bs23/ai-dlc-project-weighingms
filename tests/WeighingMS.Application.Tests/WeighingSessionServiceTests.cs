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

/// <summary>Bolt 007 use cases with hand-written fakes at every port.</summary>
public sealed class WeighingSessionServiceTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 5, 1, 12, 31, TimeSpan.Zero));
    private readonly FakeOrgatex _orgatex = new();
    private readonly FakeSnapshots _snapshots = new();
    private readonly FakeSessions _sessions = new();
    private readonly FakeTolerances _tolerances = new();
    private readonly FakeScales _scales = new();
    private readonly FakeSelection _selection = new();
    private readonly FakeAudit _audit = new();
    private readonly WeighingSessionService _service;

    public WeighingSessionServiceTests()
    {
        var clock = new FakePlantClock(_time);
        var devices = new DeviceService(_scales, new NoScanner(), [], new DeviceHealthTracker(_time), new DevicesOptions(), _time);
        _service = new WeighingSessionService(_sessions, new BatchService(_orgatex, _snapshots, clock), _tolerances, _scales, devices, _selection, _audit, clock);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<WeighingSession> StartedAsync(string batchId = "B-24187")
    {
        var started = await _service.StartOrResumeAsync(batchId, "OP-1001", Ct);
        started.IsError.ShouldBeFalse(started.IsError ? started.FirstError.Code : null);
        _audit.Entries.Clear();
        _sessions.Saves = 0;
        return started.Value.Session;
    }

    [Fact]
    public async Task Start_FirstOpen_CreatesTheSessionAndAuditsIt()
    {
        var result = await _service.StartOrResumeAsync("B-24187", "OP-1001", Ct);

        result.Value.Started.ShouldBeTrue();
        var session = result.Value.Session;
        session.Components.Count.ShouldBe(5);
        session.Components.ShouldAllBe(c => c.State == ComponentState.Pending);
        session.Events.ShouldBeEmpty();
        _sessions.Stored.ShouldHaveSingleItem().ShouldBeSameAs(session);
        _audit.Entries.ShouldHaveSingleItem().Type.ShouldBe(AuditEventType.WeighingSessionStarted);
        _audit.Entries[0].Subject.ShouldBe("B-24187");
        _audit.Entries[0].ActorBadgeId.ShouldBe("OP-1001");
    }

    [Fact]
    public async Task Start_ReadsTheScaleListOnceForUnitAndResolution()
    {
        var session = (await _service.StartOrResumeAsync("B-24187", "OP-1001", Ct)).Value.Session;

        _scales.ListCalls.ShouldBe(1);
        session.Components[2].Scale.Resolution.ShouldBe(Quantity.Of(0.1m, MassUnit.Gram));
        session.Components[3].Scale.Unit.ShouldBe(MassUnit.Kilogram);
    }

    [Fact]
    public async Task Start_RecipeNotYetSnapshotted_IsLoadedThroughTheBatchService()
    {
        await _service.StartOrResumeAsync("B-24187", "OP-1001", Ct);

        _orgatex.RecipeCalls.ShouldBe(1);
        _snapshots.Stored.ShouldHaveSingleItem().BatchId.ShouldBe("B-24187");
    }

    [Fact]
    public async Task Start_OrgatexDownAndNoSnapshot_IsUnavailableAndStoresNothing()
    {
        _orgatex.Down = true;

        var result = await _service.StartOrResumeAsync("B-24187", "OP-1001", Ct);

        result.FirstError.Code.ShouldBe("OrgaTex.Unavailable");
        _sessions.Stored.ShouldBeEmpty();
        _audit.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Start_ScaleListDown_IsUnavailableAndStoresNothing()
    {
        _scales.Down = true;

        var result = await _service.StartOrResumeAsync("B-24187", "OP-1001", Ct);

        result.FirstError.Code.ShouldBe("Scale.Unavailable");
        _sessions.Stored.ShouldBeEmpty();
    }

    [Fact]
    public async Task Start_AssignedScaleNotAtTheStation_IsScaleNotAvailable()
    {
        _selection.Policy = ScaleSelectionPolicy.Create(3m, "scale-3kg", "scale-99kg").Value;

        var result = await _service.StartOrResumeAsync("B-24187", "OP-1001", Ct);

        result.FirstError.Code.ShouldBe("Weighing.ScaleNotAvailable");
        _sessions.Stored.ShouldBeEmpty();
    }

    [Fact]
    public async Task Start_InvalidPolicy_IsReturned()
    {
        _selection.Error = Error.Validation("Weighing.InvalidScalePolicy", "bad");

        (await _service.StartOrResumeAsync("B-24187", "OP-1001", Ct)).FirstError.Code.ShouldBe("Weighing.InvalidScalePolicy");
    }

    [Fact]
    public async Task Start_NoRecipeTolerance_AsksTheCatalogueForChemicalAndWireScaleClass()
    {
        _orgatex.SodaAshTolerance = null;
        _tolerances.Entries[("AUX-0107", "heavyDuty")] = Quantity.Of(0.048m, MassUnit.Kilogram);

        var soda = (await _service.StartOrResumeAsync("B-24187", "OP-1001", Ct)).Value.Session.Components[4];

        soda.ToleranceSource.ShouldBe(ToleranceSource.Configured);
        soda.Tolerance.PlusMinus.ShouldBe(Quantity.Of(0.048m, MassUnit.Kilogram));
    }

    [Fact]
    public async Task Start_NoToleranceAnywhere_StartsWithTheComponentBlocked()
    {
        _orgatex.SodaAshTolerance = null;

        var session = (await _service.StartOrResumeAsync("B-24187", "OP-1001", Ct)).Value.Session;

        session.Components[4].Tolerance.IsBlocked.ShouldBeTrue();
    }

    [Fact]
    public async Task Resume_ExistingSession_NeedsNeitherOrgatexNorAScale()
    {
        await StartedAsync();
        _orgatex.Down = true;
        _scales.Down = true;
        var listCalls = _scales.ListCalls;

        var result = await _service.StartOrResumeAsync("B-24187", "OP-1002", Ct);

        result.Value.Started.ShouldBeFalse();
        _scales.ListCalls.ShouldBe(listCalls);
        _audit.Entries.ShouldHaveSingleItem().Type.ShouldBe(AuditEventType.WeighingSessionResumed);
        _audit.Entries[0].ActorBadgeId.ShouldBe("OP-1002");
        _sessions.Saves.ShouldBe(1);
    }

    [Fact]
    public async Task Resume_ConfigurationChangedMeanwhile_KeepsTheScaleAndToleranceOfTheOpenSession()
    {
        await StartedAsync();
        _selection.Policy = ScaleSelectionPolicy.Create(0.05m, "scale-3kg", "scale-32kg").Value;

        var yellow = (await _service.StartOrResumeAsync("B-24187", "OP-1001", Ct)).Value.Session.Components[2];

        yellow.ScaleId.ShouldBe("scale-3kg"); // A1: the 94.0 g component stays on the precision scale
    }

    [Fact]
    public async Task Start_LosingARaceToAnotherFirstOpen_ResumesTheWinnersSession()
    {
        var winner = TestSession();
        _sessions.RaceWinner = winner;

        var result = await _service.StartOrResumeAsync("B-24187", "OP-1002", Ct);

        result.Value.Started.ShouldBeFalse();
        result.Value.Session.ShouldBeSameAs(winner);
        _audit.Entries.Select(e => e.Type).ShouldContain(AuditEventType.WeighingSessionResumed);
    }

    [Fact]
    public async Task Get_NoSession_IsNotFound() =>
        (await _service.GetAsync("B-24187", Ct)).FirstError.Code.ShouldBe("Weighing.SessionNotFound");

    [Fact]
    public async Task Select_Pending_IsSavedAndAudited()
    {
        await StartedAsync();

        var result = await _service.SelectAsync("B-24187", 3, "OP-1001", Ct);

        result.Value.ActiveSequence.ShouldBe(3);
        _sessions.Saves.ShouldBe(1);
        _audit.Entries.ShouldHaveSingleItem().Type.ShouldBe(AuditEventType.ComponentSelected);
        _audit.Entries[0].Subject.ShouldBe("B-24187#3");
    }

    [Fact]
    public async Task Select_Switching_AuditsTheAbandonedAttemptThenTheSelection()
    {
        await StartedAsync();
        await _service.SelectAsync("B-24187", 3, "OP-1001", Ct);
        _audit.Entries.Clear();

        await _service.SelectAsync("B-24187", 4, "OP-1001", Ct);

        _audit.Entries.Select(e => (e.Type, e.Subject)).ShouldBe([(AuditEventType.AttemptAbandoned, "B-24187#3"), (AuditEventType.ComponentSelected, "B-24187#4")]);
    }

    [Fact]
    public async Task Select_TheActiveOneAgain_SavesNothing()
    {
        await StartedAsync();
        await _service.SelectAsync("B-24187", 3, "OP-1001", Ct);
        _sessions.Saves = 0;
        _audit.Entries.Clear();

        (await _service.SelectAsync("B-24187", 3, "OP-1001", Ct)).IsError.ShouldBeFalse();

        _sessions.Saves.ShouldBe(0);
        _audit.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Select_NoSession_IsNotFound() =>
        (await _service.SelectAsync("B-24187", 1, "OP-1001", Ct)).FirstError.Code.ShouldBe("Weighing.SessionNotFound");

    [Fact]
    public async Task Select_UnknownSequence_IsNotFoundAndNotSaved()
    {
        await StartedAsync();

        (await _service.SelectAsync("B-24187", 9, "OP-1001", Ct)).FirstError.Code.ShouldBe("Weighing.ComponentNotFound");
        _sessions.Saves.ShouldBe(0);
    }

    [Fact]
    public async Task Select_ConcurrentChange_IsAConflict()
    {
        await StartedAsync();
        _sessions.Conflict = true;

        (await _service.SelectAsync("B-24187", 3, "OP-1001", Ct)).FirstError.Code.ShouldBe("Concurrency.Conflict");
    }

    [Fact]
    public async Task Read_NoActiveComponent_IsAConflict()
    {
        await StartedAsync();

        (await _service.ReadCurrentAsync("B-24187", Ct)).FirstError.Code.ShouldBe("Weighing.NoActiveComponent");
    }

    [Fact]
    public async Task Read_NoSession_IsNotFound() =>
        (await _service.ReadCurrentAsync("B-24187", Ct)).FirstError.Code.ShouldBe("Weighing.SessionNotFound");

    [Theory]
    [InlineData(3, "scale-3kg", 93.4, "g", WeightClass.Underweight)]
    [InlineData(3, "scale-3kg", 93.5, "g", WeightClass.WithinRange)]
    [InlineData(3, "scale-3kg", 94.0, "g", WeightClass.TargetAchieved)]
    [InlineData(3, "scale-3kg", 94.6, "g", WeightClass.Overweight)]
    [InlineData(4, "scale-32kg", 24.048, "kg", WeightClass.WithinRange)]
    public async Task Read_ActiveComponent_ReadsItsOwnScaleAndClassifies(int sequence, string scaleId, decimal net, string unit, WeightClass expected)
    {
        await StartedAsync();
        await _service.SelectAsync("B-24187", sequence, "OP-1001", Ct);
        _scales.Reading = new RawScaleReading(scaleId, net, unit, true, false, _time.GetUtcNow());

        var result = await _service.ReadCurrentAsync("B-24187", Ct);

        _scales.LastReadScale.ShouldBe(scaleId);
        result.Value.Classification!.Class.ShouldBe(expected);
        result.Value.Component.Sequence.ShouldBe(sequence);
    }

    [Fact]
    public async Task Read_BelowZero_IsUnderweightWithTheFullTargetRemaining()
    {
        await StartedAsync();
        await _service.SelectAsync("B-24187", 3, "OP-1001", Ct);
        _scales.Reading = new RawScaleReading("scale-3kg", -3.0m, "g", true, false, _time.GetUtcNow());

        var result = await _service.ReadCurrentAsync("B-24187", Ct);

        result.Value.Reading.Validity.ShouldBe(ReadingValidity.BelowZero);
        result.Value.Classification!.Class.ShouldBe(WeightClass.Underweight);
        result.Value.Classification.Remaining.ShouldBe(Quantity.Of(94.0m, MassUnit.Gram));
    }

    [Theory]
    [InlineData(null, "g", false)]     // malformed: no value
    [InlineData(94.0, null, false)]    // malformed: no unit
    [InlineData(3300.0, "g", true)]    // over capacity
    public async Task Read_UnusableReading_HasNoClass(double? net, string? unit, bool overCapacity)
    {
        await StartedAsync();
        await _service.SelectAsync("B-24187", 3, "OP-1001", Ct);
        _scales.Reading = new RawScaleReading("scale-3kg", net is { } n ? (decimal)n : null, unit, true, overCapacity, _time.GetUtcNow());

        (await _service.ReadCurrentAsync("B-24187", Ct)).Value.Classification.ShouldBeNull();
    }

    [Fact]
    public async Task Read_BlockedTolerance_HasNoClass()
    {
        _orgatex.SodaAshTolerance = null;
        await StartedAsync();
        await _service.SelectAsync("B-24187", 5, "OP-1001", Ct);
        _scales.Reading = new RawScaleReading("scale-32kg", 9.600m, "kg", true, false, _time.GetUtcNow());

        var result = await _service.ReadCurrentAsync("B-24187", Ct);

        result.Value.Classification.ShouldBeNull();
        result.Value.Component.Tolerance.IsBlocked.ShouldBeTrue();
    }

    [Fact]
    public async Task Read_ScaleDown_IsUnavailable()
    {
        await StartedAsync();
        await _service.SelectAsync("B-24187", 3, "OP-1001", Ct);
        _scales.Down = true;

        (await _service.ReadCurrentAsync("B-24187", Ct)).FirstError.Code.ShouldBe("Scale.Unavailable");
    }

    [Theory]
    [InlineData(ScaleClass.Precision, "precision")]
    [InlineData(ScaleClass.HeavyDuty, "heavyDuty")]
    public void WireScaleClass_MatchesTheToleranceConfiguration(ScaleClass scaleClass, string wire) =>
        WeighingSessionService.WireScaleClass(scaleClass).ShouldBe(wire);

    private WeighingSession TestSession()
    {
        var recipe = RecipeSnapshot.Create(
            new BatchHeader("B-24187", "SO-9931", "JET-04", "100% CTN knit", 480m, "Teal 4C", false),
            _orgatex.Lines(),
            _time.GetUtcNow()).Value;
        var scales = new Dictionary<string, ScaleProfile>
        {
            ["scale-3kg"] = new("scale-3kg", MassUnit.Gram, Quantity.Of(0.1m, MassUnit.Gram)),
            ["scale-32kg"] = new("scale-32kg", MassUnit.Kilogram, Quantity.Of(0.001m, MassUnit.Kilogram)),
        };
        return WeighingSession.Start(recipe, _selection.Policy, scales, (_, _) => null, "OP-1001", _time.GetUtcNow()).Value;
    }

    private sealed class FakeOrgatex : IOrgatexGateway
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

    private sealed class FakeSnapshots : IRecipeSnapshotStore
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

    private sealed class FakeSessions : IWeighingSessionRepository
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

    private sealed class FakeTolerances : IToleranceCatalogue
    {
        public Dictionary<(string, string), Quantity> Entries { get; } = [];

        public Quantity? Find(string chemicalId, string scaleClass) => Entries.GetValueOrDefault((chemicalId, scaleClass));
    }

    private sealed class FakeSelection : IScaleSelectionSource
    {
        public ScaleSelectionPolicy Policy { get; set; } = ScaleSelectionPolicy.Create(3.0m, "scale-3kg", "scale-32kg").Value;

        public Error? Error { get; set; }

        public ErrorOr<ScaleSelectionPolicy> Current() => Error is { } e ? e : Policy;
    }

    private sealed class FakeScales : IScaleDevice
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

        public Task<ErrorOr<TareResult>> TareAsync(string scaleId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class NoScanner : IScanner
    {
        public Task<ErrorOr<IReadOnlyList<ScanEvent>>> GetScansAfterAsync(long after, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ErrorOr<DeviceStatus>> GetStatusAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
