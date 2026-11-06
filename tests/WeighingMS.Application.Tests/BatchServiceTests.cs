using ErrorOr;
using Microsoft.Extensions.Time.Testing;
using WeighingMS.Application.Batches;
using WeighingMS.Domain.Batches;
using WeighingMS.Domain.WriteBack;

namespace WeighingMS.Application.Tests;

public sealed class BatchServiceTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 3, 21, 48, 22, TimeSpan.Zero));
    private readonly FakeOrgatex _orgatex = new();
    private readonly FakeSnapshots _snapshots = new();
    private readonly FakeWriteBacks _writeBacks = new();
    private readonly BatchService _service;

    public BatchServiceTests() => _service = new BatchService(_orgatex, _snapshots, new FakePlantClock(_time), _writeBacks);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("pending", BatchStatus.SyncPending)]
    [InlineData("sent", BatchStatus.Complete)]
    [InlineData("failed", BatchStatus.SyncFailed)]
    public async Task Queue_ACompletedBatch_TakesItsStatusFromTheWriteBack(string state, BatchStatus expected)
    {
        await _service.GetRecipeAsync("B-24187", Ct); // opened: without a write-back it would be Weighing
        var now = _time.GetUtcNow();
        var writeBack = ConsumptionWriteBack.Create("B-24187", Guid.CreateVersion7(), [new("DYE-4471", 94.2m, "g", "L-1")], now);
        if (state == "sent")
        {
            writeBack.RecordSent(now, now);
        }
        else if (state == "failed")
        {
            writeBack.RecordFailure("x", retryable: false, new RetryPolicy(1, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)), now);
        }

        _writeBacks.Add(writeBack);

        var queue = (await _service.GetQueueAsync(Ct)).Value;
        queue.Single(q => q.Batch.BatchId == "B-24187").Status.ShouldBe(expected);
        queue.Single(q => q.Batch.BatchId == "B-24188").Status.ShouldBe(BatchStatus.Queued);
        (await _service.GetBatchAsync("B-24187", Ct)).Value.Status.ShouldBe(expected);
    }

    [Fact]
    public async Task Queue_BeforeAndAfterOpening_IsQueuedThenWeighing()
    {
        (await _service.GetQueueAsync(Ct)).Value.Single(q => q.Batch.BatchId == "B-24187").Status.ShouldBe(BatchStatus.Queued);

        await _service.GetRecipeAsync("B-24187", Ct);

        var queue = (await _service.GetQueueAsync(Ct)).Value;
        queue.Single(q => q.Batch.BatchId == "B-24187").Status.ShouldBe(BatchStatus.Weighing);
        queue.Single(q => q.Batch.BatchId == "B-24188").Status.ShouldBe(BatchStatus.Queued);
        (await _service.GetBatchAsync("B-24187", Ct)).Value.Status.ShouldBe(BatchStatus.Weighing);
    }

    [Fact]
    public async Task Recipe_FirstCall_SnapshotsAndSecondCallDoesNotAskOrgatex()
    {
        var first = (await _service.GetRecipeAsync("B-24187", Ct)).Value;
        _orgatex.RecipeCalls.ShouldBe(1);
        _time.Advance(TimeSpan.FromMinutes(1));

        var second = (await _service.GetRecipeAsync("B-24187", Ct)).Value;

        _orgatex.RecipeCalls.ShouldBe(1);
        second.LoadedAt.ShouldBe(first.LoadedAt);
        first.LoadedAt.ShouldBe(_time.GetUtcNow().AddMinutes(-1));
        _snapshots.Stored.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Recipe_OrgatexDownAfterSnapshot_IsStillServed()
    {
        await _service.GetRecipeAsync("B-24187", Ct);
        _orgatex.Down = true;

        (await _service.GetRecipeAsync("B-24187", Ct)).IsError.ShouldBeFalse();
        (await _service.GetQueueAsync(Ct)).FirstError.Code.ShouldBe("OrgaTex.Unavailable");
    }

    [Fact]
    public async Task Recipe_OrgatexDownWithoutSnapshot_Is503AndStoresNothing()
    {
        _orgatex.Down = true;

        (await _service.GetRecipeAsync("B-24188", Ct)).FirstError.Code.ShouldBe("OrgaTex.Unavailable");
        _snapshots.Stored.ShouldBeEmpty();
    }

    [Fact]
    public async Task Recipe_InvalidOrgatexData_Is502AndStoresNothing()
    {
        _orgatex.BadUnit = true;

        var result = await _service.GetRecipeAsync("B-24187", Ct);

        result.FirstError.Code.ShouldBe("OrgaTex.InvalidRecipe");
        _snapshots.Stored.ShouldBeEmpty();
    }

    [Fact]
    public async Task Recipe_UnknownBatch_IsNotFound()
    {
        (await _service.GetRecipeAsync("B-00000", Ct)).FirstError.Code.ShouldBe("Batch.NotFound");
        (await _service.GetBatchAsync("B-00000", Ct)).FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Recipe_TrainingOnlyFromBatchOrRecipe_IsKept()
    {
        _orgatex.RecipeTrainingOnly = true;

        (await _service.GetRecipeAsync("B-24187", Ct)).Value.TrainingOnly.ShouldBeTrue();
    }

    private sealed class FakeOrgatex : IOrgatexGateway
    {
        private readonly List<OrgatexBatch> _batches =
        [
            new("B-24187", "SO-9931", "JET-04", "100% CTN knit", 480m, "Teal 4C", 1, false),
            new("B-24188", "SO-9931", "JET-07", "100% CTN knit", 520m, "Teal 4C", 1, true),
        ];

        public bool Down { get; set; }

        public bool BadUnit { get; set; }

        public bool RecipeTrainingOnly { get; set; }

        public int RecipeCalls { get; private set; }

        public Task<ErrorOr<IReadOnlyList<OrgatexBatch>>> GetBatchesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<ErrorOr<IReadOnlyList<OrgatexBatch>>>(Down ? OrgatexErrors.Unavailable : _batches);

        public Task<ErrorOr<OrgatexBatch>> GetBatchAsync(string batchId, CancellationToken cancellationToken)
        {
            if (Down)
            {
                return Task.FromResult<ErrorOr<OrgatexBatch>>(OrgatexErrors.Unavailable);
            }

            var batch = _batches.FirstOrDefault(b => b.BatchId == batchId);
            return Task.FromResult<ErrorOr<OrgatexBatch>>(batch is null ? BatchErrors.NotFound(batchId) : batch);
        }

        public Task<ErrorOr<OrgatexRecipe>> GetRecipeAsync(string batchId, CancellationToken cancellationToken)
        {
            RecipeCalls++;
            if (Down)
            {
                return Task.FromResult<ErrorOr<OrgatexRecipe>>(OrgatexErrors.Unavailable);
            }

            var line = new RecipeLine(1, "DYE-4471", "DYE-4471", "Remazol Yellow RGB", "#d9a520", 94.0m, BadUnit ? "lb" : "g", 0.5m, "C-12");
            return Task.FromResult<ErrorOr<OrgatexRecipe>>(new OrgatexRecipe(batchId, "Teal 4C", RecipeTrainingOnly, [line]));
        }

        public Task<ErrorOr<DateTimeOffset>> PostConsumptionAsync(string batchId, Guid idempotencyKey, IReadOnlyList<ConsumptionLine> lines, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
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
}
