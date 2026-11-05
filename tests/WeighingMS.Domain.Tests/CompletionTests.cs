using WeighingMS.Domain.Common;
using WeighingMS.Domain.Weighing;
using static WeighingMS.Domain.Tests.TestRecipes;

namespace WeighingMS.Domain.Tests;

/// <summary>Story 013 / AUTO-COMPLETE / A17: completion only when all N are recorded; then frozen.</summary>
public sealed class CompletionTests
{
    private static readonly DateTimeOffset _online = Now.AddMinutes(-30);

    private static void Weigh(WeighingSession session, ComponentWeighing c, string op = "OP-1001", Quantity? net = null)
    {
        session.Select(c.Sequence, op, Now);
        session.SetLot(c.Sequence, $"L{c.Sequence}", op, Now);
        session.SetContainer(c.Sequence, c.SuggestedContainer ?? "X", op, Now);
        session.RecordTare(c.Sequence, new TareTaken(c.ScaleId, Quantity.Of(0m, c.Scale.Unit), Now), op);
        session.Confirm(c.Sequence, new MeasuredReading(net ?? c.Target, true, true, _online), op, Now.AddMinutes(c.Sequence)).IsError.ShouldBeFalse();
    }

    [Fact]
    public void Complete_WhileAnyIsPending_IsIncomplete()
    {
        var session = Start();
        Weigh(session, session.Components[0]);

        var result = session.Complete("OP-1001", Now);

        result.FirstError.Code.ShouldBe("Batch.Incomplete");
        result.FirstError.Metadata!["done"].ShouldBe(1);
        result.FirstError.Metadata["total"].ShouldBe(5);
        session.Status.ShouldBe(WeighingSessionStatus.InProgress);
    }

    [Fact]
    public void LastRecord_CompletesTheBatchAutomaticallyAndRaisesBatchCompletedOnce()
    {
        var session = Start();
        foreach (var c in session.Components.Take(4).ToList())
        {
            Weigh(session, c);
        }

        session.ClearEvents();
        Weigh(session, session.Components[4], "OP-1002");

        session.Status.ShouldBe(WeighingSessionStatus.Completed);
        session.CompletedAt.ShouldBe(Now.AddMinutes(5));
        session.CompletedBy.ShouldBe("OP-1002");
        session.Active.ShouldBeNull();
        var completed = session.Events.OfType<BatchCompleted>().ShouldHaveSingleItem();
        completed.BatchId.ShouldBe("B-24187");
        completed.Records.Select(r => r.Sequence).ShouldBe([1, 2, 3, 4, 5]);
        completed.OccurredAt.ShouldBe(Now.AddMinutes(5));
    }

    [Fact]
    public void LastRecord_ByOverride_AlsoCompletes()
    {
        var session = Start();
        foreach (var c in session.Components.Take(4).ToList())
        {
            Weigh(session, c);
        }

        var soda = session.Components[4];
        session.Select(5, "OP-1001", Now);
        session.SetLot(5, "L5", "OP-1001", Now);
        session.SetContainer(5, "D-05", "OP-1001", Now);
        session.RecordTare(5, new TareTaken("scale-32kg", Kg(0m), Now), "OP-1001");
        session.Override(5, new MeasuredReading(Kg(9.700m), true, true, _online), new OverrideAuthority(Guid.CreateVersion7(), "SV-2001", "M. Rahman"), "ok", "OP-1001", Now).IsError.ShouldBeFalse();

        session.Status.ShouldBe(WeighingSessionStatus.Completed);
        soda.Record!.IsOverride.ShouldBeTrue();
    }

    [Fact]
    public void Complete_WhenAlreadyComplete_IsIdempotent()
    {
        var session = Start();
        foreach (var c in session.Components.ToList())
        {
            Weigh(session, c);
        }

        session.ClearEvents();

        session.Complete("OP-1001", Now.AddHours(1)).IsError.ShouldBeFalse();

        session.Events.ShouldBeEmpty();
        session.CompletedAt.ShouldBe(Now.AddMinutes(5));
    }

    [Fact]
    public void CompletedSession_IsFrozen() // A17
    {
        var session = Start();
        foreach (var c in session.Components.ToList())
        {
            Weigh(session, c);
        }

        session.ClearEvents();
        var reading = new MeasuredReading(G(94m), true, true, _online);
        var grant = new OverrideAuthority(Guid.CreateVersion7(), "SV-2001", "M. Rahman");

        session.Select(1, "OP-1001", Now).FirstError.Code.ShouldBe("Weighing.BatchCompleted");
        session.SetLot(1, "L", "OP-1001", Now).FirstError.Code.ShouldBe("Weighing.BatchCompleted");
        session.SetContainer(1, "C", "OP-1001", Now).FirstError.Code.ShouldBe("Weighing.BatchCompleted");
        session.RecordTare(1, new TareTaken("scale-3kg", G(0m), Now), "OP-1001").FirstError.Code.ShouldBe("Weighing.BatchCompleted");
        session.Confirm(3, reading, "OP-1001", Now).FirstError.Code.ShouldBe("Weighing.BatchCompleted");
        session.Override(3, reading, grant, "x", "OP-1001", Now).FirstError.Code.ShouldBe("Weighing.BatchCompleted");
        session.Quit(3, "OP-1001", Now).FirstError.Code.ShouldBe("Weighing.BatchCompleted");
        session.Leave("OP-1001", Now);
        session.Resume("OP-1002", Now);
        session.Events.ShouldBeEmpty();
        session.Status.ShouldBe(WeighingSessionStatus.Completed);
    }
}
