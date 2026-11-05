using WeighingMS.Domain.Weighing;
using static WeighingMS.Domain.Tests.TestRecipes;

namespace WeighingMS.Domain.Tests;

/// <summary>Decision TOL (FR-12): recipe → configured (chemical + scale) → blocked; no universal default.</summary>
public sealed class ToleranceResolverTests
{
    [Fact]
    public void Resolve_RecipeTolerance_IsUsedAndConfigurationNotConsulted()
    {
        var consulted = false;

        var result = ToleranceResolver.Resolve(G(0.5m), () =>
        {
            consulted = true;
            return G(9m);
        });

        result.PlusMinus.ShouldBe(G(0.5m));
        result.Source.ShouldBe(ToleranceSource.Recipe);
        result.IsBlocked.ShouldBeFalse();
        consulted.ShouldBeFalse();
    }

    [Fact]
    public void Resolve_NoRecipeTolerance_UsesTheConfiguredOne()
    {
        var result = ToleranceResolver.Resolve(null, () => Kg(0.048m));

        result.PlusMinus.ShouldBe(Kg(0.048m));
        result.Source.ShouldBe(ToleranceSource.Configured);
    }

    [Fact]
    public void Resolve_Neither_IsBlocked()
    {
        var result = ToleranceResolver.Resolve(null, () => null);

        result.IsBlocked.ShouldBeTrue();
        result.PlusMinus.ShouldBeNull();
        result.Source.ShouldBeNull();
    }

    [Fact]
    public void Resolve_ConfiguredZero_IsBlockedNotADefault() =>
        ToleranceResolver.Resolve(null, () => G(0m)).IsBlocked.ShouldBeTrue();

    [Fact]
    public void Resolved_ZeroTolerance_IsRejected() =>
        Should.Throw<ArgumentOutOfRangeException>(() => ToleranceResolution.Resolved(G(0m), ToleranceSource.Recipe));
}
