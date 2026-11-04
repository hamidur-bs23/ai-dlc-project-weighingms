using WeighingMS.Simulators.Host.Seed;

namespace WeighingMS.Simulators.Tests;

public sealed class SeedLoaderTests
{
    private static SeedComponent Component(int seq, decimal target = 94.0m, string unit = "g") =>
        new(seq, "DYE-4471", "DYE-4471", "Remazol Yellow RGB", "#d9a520", target, unit, 0.5m, "C-12");

    private static SeedBatch Batch(string id, params SeedComponent[] components) =>
        new(id, "SO-1", "JET-01", "fabric", 100m, "shade", "QUEUED", false, components);

    private static readonly SeedScale _scale = new("scale-3kg", "3 kg precision", "precision", "g", 3200m, 0.1m, 214m);

    [Fact]
    public void Load_RepositorySeed_LoadsFourBatchesAndTwoScales()
    {
        var seed = SeedLoader.Load(Path.Combine(AppContext.BaseDirectory, "Seed", "seed.json"));

        seed.Batches.Count.ShouldBe(4);
        seed.Scales.Count.ShouldBe(2);
    }

    [Fact]
    public void Load_MissingFile_Throws()
    {
        Should.Throw<InvalidOperationException>(() => SeedLoader.Load("does-not-exist.json"));
    }

    [Fact]
    public void Validate_DuplicateBatchIds_Throws()
    {
        var seed = new SimulatorSeed([Batch("B-1", Component(1)), Batch("B-1", Component(1))], [_scale]);

        Should.Throw<InvalidOperationException>(() => SeedLoader.Validate(seed)).Message.ShouldContain("duplicate batch");
    }

    [Theory]
    [InlineData("0", "g")]
    [InlineData("-1", "g")]
    [InlineData("94.0", "lb")]
    public void Validate_InvalidComponent_Throws(string target, string unit)
    {
        var seed = new SimulatorSeed(
            [Batch("B-1", Component(1, decimal.Parse(target, System.Globalization.CultureInfo.InvariantCulture), unit))], [_scale]);

        Should.Throw<InvalidOperationException>(() => SeedLoader.Validate(seed));
    }

    [Fact]
    public void Validate_DuplicateSequence_Throws()
    {
        var seed = new SimulatorSeed([Batch("B-1", Component(1), Component(1))], [_scale]);

        Should.Throw<InvalidOperationException>(() => SeedLoader.Validate(seed)).Message.ShouldContain("duplicate sequence");
    }

    [Fact]
    public void Validate_NoScales_Throws()
    {
        var seed = new SimulatorSeed([Batch("B-1", Component(1))], []);

        Should.Throw<InvalidOperationException>(() => SeedLoader.Validate(seed)).Message.ShouldContain("no scales");
    }
}
