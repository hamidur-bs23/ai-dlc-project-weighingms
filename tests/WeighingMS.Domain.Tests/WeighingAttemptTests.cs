using WeighingMS.Domain.Weighing;
using static WeighingMS.Domain.Tests.TestRecipes;

namespace WeighingMS.Domain.Tests;

/// <summary>Bolt 008: lot, container, tare, confirm, progression, quit and leave on the session aggregate.</summary>
public sealed class WeighingAttemptTests
{
    private static readonly DateTimeOffset _online = Now.AddMinutes(-30);

    private static MeasuredReading Stable(decimal grams) => new(G(grams), Stable: true, Valid: true, OnlineSince: _online);

    private static WeighingSession ReadyYellow(WeighingSession? session = null)
    {
        session ??= Start();
        session.Select(3, "OP-1001", Now).IsError.ShouldBeFalse();
        session.SetLot(3, "2026-0387", "OP-1001", Now).IsError.ShouldBeFalse();
        session.SetContainer(3, "C-12", "OP-1001", Now).IsError.ShouldBeFalse();
        session.RecordTare(3, new TareTaken("scale-3kg", G(214.0m), Now), "OP-1001").IsError.ShouldBeFalse();
        session.ClearEvents();
        return session;
    }

    private static List<string?> Fields(IEnumerable<ErrorOr.Error> errors) => [.. errors.Select(e => e.Metadata?["field"] as string)];

    [Fact]
    public void Select_StartsAnEmptyAttempt()
    {
        var session = Start();

        session.Select(3, "OP-1002", Now);

        session.Active!.Attempt.ShouldBe(new WeighingAttempt(Now, "OP-1002", null, null, null));
    }

    [Fact]
    public void PendingAndDoneComponents_HaveNoAttempt()
    {
        var session = Start();

        session.Components.ShouldAllBe(c => c.Attempt == null);
    }

    [Theory]
    [InlineData("  2026-0387 ", "2026-0387")]
    [InlineData("ABC", "ABC")]
    [InlineData("12345678901234567890123456789012", "12345678901234567890123456789012")]
    public void SetLot_IsTrimmedAndStored(string input, string stored)
    {
        var session = Start();
        session.Select(3, "OP-1001", Now);

        session.SetLot(3, input, "OP-1001", Now).IsError.ShouldBeFalse();

        session.Active!.Attempt!.Lot.ShouldBe(stored);
        session.Events.OfType<LotCaptured>().Single().Lot.ShouldBe(stored);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123456789012345678901234567890123")]
    public void SetLotOrContainer_EmptyOrTooLong_IsAFieldError(string? input)
    {
        var session = Start();
        session.Select(3, "OP-1001", Now);

        var lot = session.SetLot(3, input, "OP-1001", Now);
        var container = session.SetContainer(3, input, "OP-1001", Now);

        lot.FirstError.Code.ShouldBe("Request.Invalid");
        lot.FirstError.Metadata!["field"].ShouldBe("lot");
        container.FirstError.Metadata!["field"].ShouldBe("container");
        session.Active!.Attempt!.Lot.ShouldBeNull();
    }

    [Fact]
    public void Commands_OnAComponentThatIsNotActive_AreRejected()
    {
        var session = Start();
        session.Select(3, "OP-1001", Now);

        session.SetLot(4, "L", "OP-1001", Now).FirstError.Code.ShouldBe("Weighing.ComponentNotActive");
        session.SetContainer(4, "C", "OP-1001", Now).FirstError.Code.ShouldBe("Weighing.ComponentNotActive");
        session.RecordTare(4, new TareTaken("scale-32kg", Kg(1.85m), Now), "OP-1001").FirstError.Code.ShouldBe("Weighing.ComponentNotActive");
        session.Quit(4, "OP-1001", Now).FirstError.Code.ShouldBe("Weighing.ComponentNotActive");
        session.Confirm(4, Stable(24m), "OP-1001", Now).FirstError.Code.ShouldBe("Weighing.ComponentNotActive");
        session.Confirm(9, Stable(24m), "OP-1001", Now).FirstError.Code.ShouldBe("Weighing.ComponentNotFound");
    }

    [Fact]
    public void RecordTare_FirstThenAgain_IsTareThenReTareWithoutARecord()
    {
        var session = Start();
        session.Select(3, "OP-1001", Now);
        var first = new TareTaken("scale-3kg", G(214.0m), Now);
        var second = new TareTaken("scale-3kg", G(215.5m), Now.AddSeconds(30));

        session.RecordTare(3, first, "OP-1001");
        session.RecordTare(3, second, "OP-1001");

        session.Active!.Attempt!.Tare.ShouldBe(second);
        session.Events.OfType<ScaleTared>().Single().Tare.ShouldBe(first);
        var retare = session.Events.OfType<ScaleReTared>().Single();
        retare.Previous.ShouldBe(first);
        retare.Tare.ShouldBe(second);
        session.DoneCount.ShouldBe(0);
        session.Components[2].Record.ShouldBeNull();
    }

    [Fact]
    public void RecordTare_ZeroTare_IsAccepted() // A7
    {
        var session = Start();
        session.Select(3, "OP-1001", Now);

        session.RecordTare(3, new TareTaken("scale-3kg", G(0m), Now), "OP-1001").IsError.ShouldBeFalse();

        session.Active!.Attempt!.Tare!.Tare.ShouldBe(G(0m));
    }

    [Fact]
    public void RecordTare_FromAnotherScale_IsAProgrammingError()
    {
        var session = Start();
        session.Select(3, "OP-1001", Now);

        Should.Throw<InvalidOperationException>(() => session.RecordTare(3, new TareTaken("scale-32kg", Kg(1.85m), Now), "OP-1001"));
    }

    [Fact]
    public void Confirm_AllConditionsMet_CreatesOneRecordWithEveryStep25Field()
    {
        var session = ReadyYellow();

        var record = session.Confirm(3, Stable(94.2m), "OP-1002", Now.AddMinutes(2)).Value;

        record.Id.Version.ShouldBe(7);
        record.SessionId.ShouldBe(session.Id);
        record.BatchId.ShouldBe("B-24187");
        record.Sequence.ShouldBe(3);
        record.ChemicalId.ShouldBe("DYE-4471");
        record.ChemicalName.ShouldBe("Remazol Yellow RGB");
        record.Required.ShouldBe(G(94.0m));
        record.Actual.ShouldBe(G(94.2m));
        record.Class.ShouldBe(WeightClass.WithinRange);
        record.Tolerance.ShouldBe(G(0.5m));
        record.ToleranceSource.ShouldBe(ToleranceSource.Recipe);
        record.ScaleId.ShouldBe("scale-3kg");
        record.ScaleClass.ShouldBe(ScaleClass.Precision);
        record.Lot.ShouldBe("2026-0387");
        record.Container.ShouldBe("C-12");
        record.Tare.ShouldBe(G(214.0m));
        record.OperatorBadgeId.ShouldBe("OP-1002");
        record.ConfirmedAt.ShouldBe(Now.AddMinutes(2));

        var yellow = session.Components[2];
        yellow.State.ShouldBe(ComponentState.Done);
        yellow.Record.ShouldBeSameAs(record);
        yellow.Attempt.ShouldBeNull();
        session.DoneCount.ShouldBe(1);
        session.Events.OfType<WeighingConfirmed>().Single().Record.ShouldBeSameAs(record);
    }

    [Fact]
    public void Confirm_TargetAchieved_IsRecordedAsSuch() =>
        ReadyYellow().Confirm(3, Stable(94.0m), "OP-1001", Now).Value.Class.ShouldBe(WeightClass.TargetAchieved);

    [Fact]
    public void Confirm_ExtraDigits_RecordsTheValueAtTheScaleResolution() =>
        ReadyYellow().Confirm(3, Stable(94.25m), "OP-1001", Now).Value.Actual.ShouldBe(G(94.3m));

    [Fact]
    public void Confirm_Twice_TheSecondIsAlreadyConfirmedAndNothingChanges() // A8
    {
        var session = ReadyYellow();
        session.Confirm(3, Stable(94.2m), "OP-1001", Now);
        session.ClearEvents();

        var again = session.Confirm(3, Stable(94.2m), "OP-1001", Now);

        again.FirstError.Code.ShouldBe("Weighing.AlreadyConfirmed");
        session.Events.ShouldBeEmpty();
        session.Active!.Sequence.ShouldBe(4);
        session.Active.Attempt!.Lot.ShouldBeNull();
    }

    [Theory]
    [InlineData(93.4, "Weighing.OutOfTolerance")]
    [InlineData(94.6, "Weighing.OutOfTolerance")]
    public void Confirm_OutOfTolerance_IsRejectedWithTheToleranceGapOnly(decimal grams, string code)
    {
        var session = ReadyYellow();

        var result = session.Confirm(3, Stable(grams), "OP-1001", Now);

        result.FirstError.Code.ShouldBe(code);
        Fields(result.Errors).ShouldBe(["inTolerance"]);
        session.Components[2].State.ShouldBe(ComponentState.Active);
        session.Events.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(93.5)]
    [InlineData(94.5)]
    public void Confirm_OnTheInclusiveBounds_IsAccepted(decimal grams) =>
        ReadyYellow().Confirm(3, Stable(grams), "OP-1001", Now).IsError.ShouldBeFalse();

    [Fact]
    public void Confirm_UnstableInTolerance_IsRejected() // A10
    {
        var result = ReadyYellow().Confirm(3, new MeasuredReading(G(94.0m), Stable: false, Valid: true, _online), "OP-1001", Now);

        result.FirstError.Code.ShouldBe("Weighing.NotConfirmable");
        Fields(result.Errors).ShouldBe(["stable"]);
    }

    [Fact]
    public void Confirm_InvalidReading_IsRejected() // A10: below zero, over capacity, malformed
    {
        var noValue = ReadyYellow().Confirm(3, new MeasuredReading(null, true, false, _online), "OP-1001", Now);

        Fields(noValue.Errors).ShouldBe(["reading", "inTolerance"]);
    }

    [Fact]
    public void Confirm_NothingCaptured_ListsEveryGapInOrder()
    {
        var session = Start();
        session.Select(3, "OP-1001", Now);

        var result = session.Confirm(3, new MeasuredReading(G(0m), false, true, _online), "OP-1001", Now);

        Fields(result.Errors).ShouldBe(["lot", "container", "tare", "stable", "inTolerance"]);
        result.FirstError.Code.ShouldBe("Weighing.NotConfirmable");
    }

    [Fact]
    public void Confirm_TareTakenBeforeTheScaleCameOnline_IsStale()
    {
        var session = ReadyYellow();

        var result = session.Confirm(3, new MeasuredReading(G(94.2m), true, true, OnlineSince: Now.AddSeconds(10)), "OP-1001", Now);

        Fields(result.Errors).ShouldBe(["tareStale"]);
    }

    [Fact]
    public void Confirm_TareTakenAtTheMomentTheScaleCameOnline_IsValid() =>
        ReadyYellow().Confirm(3, new MeasuredReading(G(94.2m), true, true, OnlineSince: Now), "OP-1001", Now).IsError.ShouldBeFalse();

    [Fact]
    public void Confirm_BlockedTolerance_IsNeverConfirmable()
    {
        var session = Start(Teal4C(sodaAshTolerance: null));
        session.Select(5, "OP-1001", Now);
        session.SetLot(5, "SA-1", "OP-1001", Now);
        session.SetContainer(5, "D-05", "OP-1001", Now);
        session.RecordTare(5, new TareTaken("scale-32kg", Kg(1.85m), Now), "OP-1001");

        var result = session.Confirm(5, new MeasuredReading(Kg(9.600m), true, true, _online), "OP-1001", Now);

        Fields(result.Errors).ShouldBe(["tolerance", "inTolerance"]);
    }

    [Fact]
    public void Confirm_ActivatesTheNextPendingAfterItWithAFreshAttempt() // NEXT-COMPONENT
    {
        var session = ReadyYellow();

        session.Confirm(3, Stable(94.2m), "OP-1001", Now);

        session.ActiveSequence.ShouldBe(4);
        session.Active!.Attempt.ShouldBe(new WeighingAttempt(Now, "OP-1001", null, null, null));
        session.Components.Where(c => c.Sequence is 1 or 2 or 5).ShouldAllBe(c => c.State == ComponentState.Pending);
    }

    [Fact]
    public void Confirm_TheLastOne_LeavesNothingActiveAndTheSessionReadyToComplete()
    {
        var session = Start();
        foreach (var c in session.Components)
        {
            var target = c.Target;
            session.Select(c.Sequence, "OP-1001", Now);
            session.SetLot(c.Sequence, $"L{c.Sequence}", "OP-1001", Now);
            session.SetContainer(c.Sequence, c.SuggestedContainer ?? "X", "OP-1001", Now);
            session.RecordTare(c.Sequence, new TareTaken(c.ScaleId, WeighingMS.Domain.Common.Quantity.Of(0m, c.Scale.Unit), Now), "OP-1001");
            session.ReadyToComplete.ShouldBeFalse();
            session.Confirm(c.Sequence, new MeasuredReading(target, true, true, _online), "OP-1001", Now).IsError.ShouldBeFalse();
        }

        session.DoneCount.ShouldBe(5);
        session.ReadyToComplete.ShouldBeTrue();
        session.Active.ShouldBeNull();
        session.Suggested.ShouldBeNull();
        session.Components.ShouldAllBe(c => c.Record != null && c.Record.Class == WeightClass.TargetAchieved);
    }

    [Fact]
    public void Select_ADoneComponent_IsRefusedAndNothingChanges()
    {
        var session = ReadyYellow();
        session.Confirm(3, Stable(94.2m), "OP-1001", Now);
        session.ClearEvents();

        session.Select(3, "OP-1001", Now).FirstError.Code.ShouldBe("Weighing.ComponentDone");

        session.Active!.Sequence.ShouldBe(4);
        session.Events.ShouldBeEmpty();
    }

    [Fact]
    public void Quit_DiscardsTheAttemptWithoutARecord()
    {
        var session = ReadyYellow();

        session.Quit(3, "OP-1001", Now).IsError.ShouldBeFalse();

        session.Components[2].State.ShouldBe(ComponentState.Pending);
        session.Components[2].Record.ShouldBeNull();
        session.Active.ShouldBeNull();
        session.Events.ShouldHaveSingleItem().ShouldBe(new WeighingAttemptQuit("B-24187", "OP-1001", 3, Now));

        session.Select(3, "OP-1001", Now);
        session.Active!.Attempt!.Tare.ShouldBeNull(); // a fresh attempt after quitting
    }

    [Fact]
    public void Leave_DiscardsTheAttemptKeepsRecordsAndIsRecordedWhileIncomplete() // LEAVE-DISCARDS
    {
        var session = ReadyYellow();
        session.Confirm(3, Stable(94.2m), "OP-1001", Now);
        session.SetLot(4, "NS-118", "OP-1001", Now);
        session.ClearEvents();

        session.Leave("OP-1001", Now);

        session.Active.ShouldBeNull();
        session.Components[3].State.ShouldBe(ComponentState.Pending);
        session.Components[2].Record!.Actual.ShouldBe(G(94.2m));
        session.Status.ShouldBe(WeighingSessionStatus.InProgress);
        session.Events.ShouldHaveSingleItem().ShouldBe(new BatchLeft("B-24187", "OP-1001", 1, 5, Now));
    }

    [Fact]
    public void SwitchingComponent_DiscardsTheCapturedAttempt()
    {
        var session = ReadyYellow();

        session.Select(1, "OP-1001", Now);
        session.Select(3, "OP-1001", Now);

        session.Active!.Attempt!.Lot.ShouldBeNull();
        session.Active.Attempt.Tare.ShouldBeNull();
    }
}
