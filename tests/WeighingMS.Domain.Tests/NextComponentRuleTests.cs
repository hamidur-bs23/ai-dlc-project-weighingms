using WeighingMS.Domain.Weighing;
using static WeighingMS.Domain.Tests.TestRecipes;

namespace WeighingMS.Domain.Tests;

/// <summary>NEXT-COMPONENT: the next pending after the confirmed one, wrapping to the start.</summary>
public sealed class NextComponentRuleTests
{
    [Theory]
    [InlineData(new[] { 1, 2, 4, 5 }, 3, 4)]   // skipped 1 (unavailable), did 3 → 4
    [InlineData(new[] { 1, 2 }, 5, 1)]         // after the last → wraps to the first pending
    [InlineData(new[] { 2, 5 }, 3, 5)]
    [InlineData(new[] { 2 }, 3, 2)]
    [InlineData(new[] { 1, 2, 3, 4, 5 }, 0, 1)]
    public void After_IsTheNextPendingWrapping(int[] pending, int confirmed, int expected)
    {
        var session = Start();
        Mark(session, pending);

        NextComponentRule.After(session.Components, confirmed).ShouldBe(expected);
    }

    [Fact]
    public void After_NothingPending_IsNull()
    {
        var session = Start();
        Mark(session, []);

        NextComponentRule.After(session.Components, 3).ShouldBeNull();
    }

    // Marks every component not in `pending` as done by confirming it at its target.
    private static void Mark(WeighingSession session, int[] pending)
    {
        foreach (var c in session.Components.Where(c => !pending.Contains(c.Sequence)).ToList())
        {
            session.Select(c.Sequence, "OP-1001", Now);
            session.SetLot(c.Sequence, "L", "OP-1001", Now);
            session.SetContainer(c.Sequence, "C", "OP-1001", Now);
            session.RecordTare(c.Sequence, new TareTaken(c.ScaleId, WeighingMS.Domain.Common.Quantity.Of(0m, c.Scale.Unit), Now), "OP-1001");
            session.Confirm(c.Sequence, new MeasuredReading(c.Target, true, true, Now), "OP-1001", Now).IsError.ShouldBeFalse();
        }

        if (session.Active is { } active)
        {
            session.Quit(active.Sequence, "OP-1001", Now);
        }
    }
}
