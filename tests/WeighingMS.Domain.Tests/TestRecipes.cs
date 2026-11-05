using WeighingMS.Domain.Batches;
using WeighingMS.Domain.Common;
using WeighingMS.Domain.Weighing;

namespace WeighingMS.Domain.Tests;

/// <summary>The seeded training scenario (B-24187, Teal 4C) and the default station scales.</summary>
internal static class TestRecipes
{
    public static readonly DateTimeOffset Now = new(2026, 11, 5, 1, 12, 31, TimeSpan.Zero);

    public static ScaleSelectionPolicy DefaultPolicy => ScaleSelectionPolicy.Create(3.0m, "scale-3kg", "scale-32kg").Value;

    public static ScaleProfile Precision => new("scale-3kg", MassUnit.Gram, Quantity.Of(0.1m, MassUnit.Gram));

    public static ScaleProfile Floor => new("scale-32kg", MassUnit.Kilogram, Quantity.Of(0.001m, MassUnit.Kilogram));

    public static IReadOnlyDictionary<string, ScaleProfile> Scales => new Dictionary<string, ScaleProfile>
    {
        ["scale-3kg"] = Precision,
        ["scale-32kg"] = Floor,
    };

    public static RecipeSnapshot Teal4C(decimal? sodaAshTolerance = 0.048m) =>
        RecipeSnapshot.Create(
            new BatchHeader("B-24187", "SO-9931", "JET-04", "100% CTN knit", 480m, "Teal 4C", false),
            [
                new(1, "DYE-1180", "DYE-1180", "Remazol Brill. Blue RSPL", "#1f4fa8", 1240.0m, "g", 6.0m, "C-09"),
                new(2, "DYE-2204", "DYE-2204", "Remazol Red RB", "#b8253a", 386.0m, "g", 2.0m, "C-11"),
                new(3, "DYE-4471", "DYE-4471", "Remazol Yellow RGB", "#d9a520", 94.0m, "g", 0.5m, "C-12"),
                new(4, "AUX-0101", "AUX-0101", "Sodium Sulphate", "#c9d2dc", 24.000m, "kg", 0.120m, "D-03"),
                new(5, "AUX-0107", "AUX-0107", "Soda Ash", "#e4eaf0", 9.600m, "kg", sodaAshTolerance, "D-05"),
            ],
            Now).Value;

    public static Quantity G(decimal value) => Quantity.Of(value, MassUnit.Gram);

    public static Quantity Kg(decimal value) => Quantity.Of(value, MassUnit.Kilogram);

    public static WeighingSession Start(RecipeSnapshot? recipe = null, Func<string, ScaleClass, Quantity?>? configured = null) =>
        WeighingSession.Start(recipe ?? Teal4C(), DefaultPolicy, Scales, configured ?? ((_, _) => null), "OP-1001", Now).Value;
}
