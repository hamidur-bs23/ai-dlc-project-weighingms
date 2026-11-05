using WeighingMS.Domain.Batches;
using WeighingMS.Domain.Common;

namespace WeighingMS.Domain.Tests;

public sealed class RecipeSnapshotTests
{
    private static readonly DateTimeOffset _loadedAt = new(2026, 11, 4, 3, 48, 22, TimeSpan.FromHours(6));
    private static readonly BatchHeader _header = new("B-24187", "SO-9931", "JET-04", "100% CTN knit", 480m, "Teal 4C", false);

    private static List<RecipeLine> Teal4C() =>
    [
        new(1, "DYE-1180", "DYE-1180", "Remazol Brill. Blue RSPL", "#1f4fa8", 1240.0m, "g", 6.0m, "C-09"),
        new(2, "DYE-2204", "DYE-2204", "Remazol Red RB", "#b8253a", 386.0m, "g", 2.0m, "C-11"),
        new(3, "DYE-4471", "DYE-4471", "Remazol Yellow RGB", "#d9a520", 94.0m, "g", 0.5m, "C-12"),
        new(4, "AUX-0101", "AUX-0101", "Sodium Sulphate", "#c9d2dc", 24.000m, "kg", 0.120m, "D-03"),
        new(5, "AUX-0107", "AUX-0107", "Soda Ash", "#e4eaf0", 9.600m, "kg", 0.048m, "D-05"),
    ];

    [Fact]
    public void Create_B24187_KeepsHeaderAndFiveComponentsInSequence()
    {
        var lines = Teal4C();
        lines.Reverse();

        var snapshot = RecipeSnapshot.Create(_header, lines, _loadedAt).Value;

        snapshot.BatchId.ShouldBe("B-24187");
        snapshot.OrderId.ShouldBe("SO-9931");
        snapshot.Machine.ShouldBe("JET-04");
        snapshot.Shade.ShouldBe("Teal 4C");
        snapshot.BatchWeightKg.ShouldBe(480m);
        snapshot.Components.Select(c => c.Code).ShouldBe(["DYE-1180", "DYE-2204", "DYE-4471", "AUX-0101", "AUX-0107"]);
        snapshot.Id.Version.ShouldBe(7);
    }

    [Fact]
    public void Create_YellowRgb_HasTargetToleranceAndContainer()
    {
        var yellow = RecipeSnapshot.Create(_header, Teal4C(), _loadedAt).Value.Components[2];

        yellow.Target.ShouldBe(Quantity.Create(94.0m, "g").Value);
        yellow.RecipeTolerance.ShouldBe(Quantity.Create(0.5m, "g").Value);
        yellow.SuggestedContainer.ShouldBe("C-12");
        yellow.Name.ShouldBe("Remazol Yellow RGB");
    }

    [Fact]
    public void Create_ToleranceAbsent_RecipeToleranceIsNull()
    {
        var line = new RecipeLine(1, "AUX-0204", "AUX-0204", "Sequestering Agent", "#dfe7ef", 0.930m, "kg", null, " ");

        var component = RecipeSnapshot.Create(_header, [line], _loadedAt).Value.Components.Single();

        component.RecipeTolerance.ShouldBeNull();
        component.SuggestedContainer.ShouldBeNull();
    }

    [Fact]
    public void Create_LoadedAt_IsStoredInUtc()
    {
        var snapshot = RecipeSnapshot.Create(_header, Teal4C(), _loadedAt).Value;

        snapshot.LoadedAt.Offset.ShouldBe(TimeSpan.Zero);
        snapshot.LoadedAt.ShouldBe(new DateTimeOffset(2026, 11, 3, 21, 48, 22, TimeSpan.Zero));
    }

    [Fact]
    public void Components_IsACopy_ThatCannotChangeTheSnapshot()
    {
        var snapshot = RecipeSnapshot.Create(_header, Teal4C(), _loadedAt).Value;

        snapshot.Components.ShouldNotBeSameAs(snapshot.Components);
        snapshot.Components.Count.ShouldBe(5);
    }

    public static TheoryData<string, List<RecipeLine>> InvalidRecipes => new()
    {
        { "no components", [] },
        { "not unique", [Line(1, 94m, "g", 0.5m), Line(1, 50m, "g", 0.5m)] },
        { "Unknown mass unit", [Line(1, 94m, "lb", 0.5m)] },
        { "greater than zero", [Line(1, 0m, "g", 0.5m)] },
        { "greater than zero", [Line(1, -1m, "g", 0.5m)] },
        { "tolerance must be greater than zero", [Line(1, 94m, "g", 0m)] },
    };

    [Theory]
    [MemberData(nameof(InvalidRecipes))]
    public void Create_InvalidRecipe_IsRejectedAs502InvalidRecipe(string reason, List<RecipeLine> lines)
    {
        var result = RecipeSnapshot.Create(_header, lines, _loadedAt);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("OrgaTex.InvalidRecipe");
        result.FirstError.NumericType.ShouldBe(502);
        result.FirstError.Description.ShouldContain(reason);
    }

    private static RecipeLine Line(int seq, decimal target, string unit, decimal? tolerance) =>
        new(seq, "DYE-4471", "DYE-4471", "Remazol Yellow RGB", "#d9a520", target, unit, tolerance, "C-12");
}
