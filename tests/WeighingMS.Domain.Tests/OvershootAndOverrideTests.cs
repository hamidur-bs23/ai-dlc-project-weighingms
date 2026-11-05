using WeighingMS.Domain.Common;
using WeighingMS.Domain.Weighing;
using static WeighingMS.Domain.Tests.TestRecipes;

namespace WeighingMS.Domain.Tests;

/// <summary>
/// Workbook STEP 24 / PD-05 option 1 (ADR-001): an overweight stays unconfirmed; it ends by coming back into range,
/// quit and re-weigh, or a supervisor override (true class + surplus + OVR). No limit; no recalculation.
/// </summary>
public sealed class OvershootAndOverrideTests
{
    private static readonly DateTimeOffset _online = Now.AddMinutes(-30);
    private static readonly OverrideAuthority _supervisor = new(Guid.CreateVersion7(), "SV-2001", "M. Rahman");

    private static MeasuredReading Stable(Quantity net) => new(net, Stable: true, Valid: true, OnlineSince: _online);

    private static WeighingSession Ready(int sequence = 3, WeighingSession? session = null)
    {
        session ??= Start();
        var c = session.Components.Single(x => x.Sequence == sequence);
        session.Select(sequence, "OP-1001", Now);
        session.SetLot(sequence, "2026-0387", "OP-1001", Now);
        session.SetContainer(sequence, c.SuggestedContainer ?? "X", "OP-1001", Now);
        session.RecordTare(sequence, new TareTaken(c.ScaleId, Quantity.Of(0m, c.Scale.Unit), Now), "OP-1001");
        session.ClearEvents();
        return session;
    }

    private static List<string?> Fields(IEnumerable<ErrorOr.Error> errors) => [.. errors.Select(e => e.Metadata?["field"] as string)];

    // ── STEP 24 rule ────────────────────────────────────────────────────────────

    [Fact]
    public void Overshoot_BlocksConfirmAndReportsTheSurplusAgainstTheTarget()
    {
        var session = Ready();

        var check = ConfirmationRules.Check(session.Active!, Stable(G(96.4m)));
        var confirm = session.Confirm(3, Stable(G(96.4m)), "OP-1001", Now);

        check.Classification!.Class.ShouldBe(WeightClass.Overweight);
        check.Classification.Surplus.ShouldBe(new Surplus(G(2.4m), 2.6m)); // "SURPLUS +2.4 g (+2.6%)"
        check.Missing.ShouldBe([ConfirmationGap.InTolerance]);
        confirm.FirstError.Code.ShouldBe("Weighing.OutOfTolerance");
        session.Components[2].State.ShouldBe(ComponentState.Active);
    }

    [Fact]
    public void Overshoot_BroughtBackIntoRange_IsConfirmedNormally() // recovery (a)
    {
        var session = Ready();
        session.Confirm(3, Stable(G(96.4m)), "OP-1001", Now).IsError.ShouldBeTrue();

        var record = session.Confirm(3, Stable(G(94.3m)), "OP-1001", Now).Value;

        record.Class.ShouldBe(WeightClass.WithinRange);
        record.IsOverride.ShouldBeFalse();
        record.Surplus.ShouldBeNull();
    }

    [Fact]
    public void Overshoot_QuitAndReWeigh_StartsAFreshAttemptWithNoRecord() // recovery (b)
    {
        var session = Ready();

        session.Quit(3, "OP-1001", Now);
        session.Select(3, "OP-1001", Now);

        session.Components[2].Record.ShouldBeNull();
        session.Active!.Attempt!.Tare.ShouldBeNull();
        session.Active.Attempt.Lot.ShouldBeNull();
    }

    [Fact]
    public void Overshoot_Override_RecordsTrueClassSurplusAndOvr() // recovery (c)
    {
        var session = Ready();

        var record = session.Override(3, Stable(G(96.4m)), _supervisor, "  Shade lab accepts +2.4 g  ", "OP-1001", Now).Value;

        record.Class.ShouldBe(WeightClass.Overweight);
        record.Actual.ShouldBe(G(96.4m));
        record.Surplus.ShouldBe(new Surplus(G(2.4m), 2.6m));
        record.IsOverride.ShouldBeTrue();
        record.Override.ShouldBe(new OverrideDetails(_supervisor.GrantId, "SV-2001", "M. Rahman", "Shade lab accepts +2.4 g", Now));
        record.OperatorBadgeId.ShouldBe("OP-1001");
        session.Events.OfType<WeighingOverridden>().Single().Record.ShouldBeSameAs(record);
        session.ActiveSequence.ShouldBe(4);
    }

    [Fact]
    public void Overshoot_LargeSurplus_CanStillBeOverridden() // option 1: no limit, by decision
    {
        var record = Ready().Override(3, Stable(G(141.0m)), _supervisor, "Accepted", "OP-1001", Now).Value;

        record.Surplus!.PercentOfTarget.ShouldBe(50.0m);
        record.IsOverride.ShouldBeTrue();
    }

    [Fact]
    public void Overshoot_NeverChangesAnotherComponentsTarget() // no recalculation (FR-16, ADR-001)
    {
        var session = Ready();
        var before = session.Components.Select(c => (c.Sequence, c.Target, c.Band)).ToList();

        session.Confirm(3, Stable(G(96.4m)), "OP-1001", Now);
        session.Override(3, Stable(G(96.4m)), _supervisor, "Accepted", "OP-1001", Now);

        session.Components.Select(c => (c.Sequence, c.Target, c.Band)).ShouldBe(before);
    }

    [Fact]
    public void Underweight_Override_RecordsUnderweightWithoutSurplus()
    {
        var record = Ready().Override(3, Stable(G(93.0m)), _supervisor, "Lot ran out", "OP-1001", Now).Value;

        record.Class.ShouldBe(WeightClass.Underweight);
        record.Surplus.ShouldBeNull();
        record.IsOverride.ShouldBeTrue();
    }

    // ── Override rules ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(94.0)]
    [InlineData(93.5)]
    [InlineData(94.5)]
    public void Override_InRange_IsNotNeeded(decimal grams)
    {
        var session = Ready();

        session.Override(3, Stable(G(grams)), _supervisor, "x", "OP-1001", Now).FirstError.Code.ShouldBe("Weighing.OverrideNotNeeded");
        session.Components[2].Record.ShouldBeNull();
    }

    [Fact]
    public void Override_BySameBadgeAsTheOperator_IsFourEyes() // OVR-FOUR-EYES
    {
        var session = Ready();

        var result = session.Override(3, Stable(G(96.4m)), _supervisor, "x", "SV-2001", Now);

        result.FirstError.Code.ShouldBe("Auth.FourEyesRequired");
        session.Components[2].Record.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Override_WithoutReason_ChangesNothing(string? reason)
    {
        var session = Ready();

        var result = session.Override(3, Stable(G(96.4m)), _supervisor, reason, "OP-1001", Now);

        Fields(result.Errors).ShouldBe(["reason"]);
        session.Components[2].State.ShouldBe(ComponentState.Active);
        session.Events.ShouldBeEmpty();
    }

    [Fact]
    public void Override_ReasonUpTo500Characters_IsAcceptedAndLongerIsNot()
    {
        Ready().Override(3, Stable(G(96.4m)), _supervisor, new string('r', 500), "OP-1001", Now).IsError.ShouldBeFalse();
        Fields(Ready().Override(3, Stable(G(96.4m)), _supervisor, new string('r', 501), "OP-1001", Now).Errors).ShouldBe(["reason"]);
    }

    [Fact]
    public void Override_StillNeedsLotContainerTareAndAStableReading() // A11
    {
        var session = Start();
        session.Select(3, "OP-1001", Now);

        var result = session.Override(3, new MeasuredReading(G(96.4m), Stable: false, Valid: true, _online), _supervisor, "x", "OP-1001", Now);

        result.FirstError.Code.ShouldBe("Weighing.NotConfirmable");
        Fields(result.Errors).ShouldBe(["lot", "container", "tare", "stable"]);
    }

    [Fact]
    public void Override_NoUsableReading_IsRejected()
    {
        var result = Ready().Override(3, new MeasuredReading(null, true, false, _online), _supervisor, "x", "OP-1001", Now);

        Fields(result.Errors).ShouldBe(["reading"]);
    }

    [Fact]
    public void Override_BlockedTolerance_IsNotPossible() // A3 still holds: only "in tolerance" is waived
    {
        var session = Ready(5, Start(Teal4C(sodaAshTolerance: null)));

        var result = session.Override(5, Stable(Kg(9.9m)), _supervisor, "x", "OP-1001", Now);

        Fields(result.Errors).ShouldContain("tolerance");
    }

    [Fact]
    public void Override_NotActiveOrDoneOrUnknown_IsRejected()
    {
        var session = Ready();
        session.Override(4, Stable(Kg(25m)), _supervisor, "x", "OP-1001", Now).FirstError.Code.ShouldBe("Weighing.ComponentNotActive");
        session.Override(9, Stable(Kg(25m)), _supervisor, "x", "OP-1001", Now).FirstError.Code.ShouldBe("Weighing.ComponentNotFound");
        session.Override(3, Stable(G(96.4m)), _supervisor, "x", "OP-1001", Now);
        session.Override(3, Stable(G(96.4m)), _supervisor, "x", "OP-1001", Now).FirstError.Code.ShouldBe("Weighing.AlreadyConfirmed");
    }

    // ── Surplus ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(94.0, 96.4, 2.4, 2.6)]
    [InlineData(94.0, 94.6, 0.6, 0.6)]
    [InlineData(420.0, 429.6, 9.6, 2.3)]
    public void Surplus_IsAgainstTheTargetAsAPercentOfTarget(decimal target, decimal net, decimal amount, decimal percent)
    {
        var c = ReadingClassifier.Classify(G(target), G(0.5m), G(net), Precision);

        c.Surplus.ShouldBe(new Surplus(G(amount), percent));
    }

    [Fact]
    public void Surplus_OnlyForOverweight()
    {
        ReadingClassifier.Classify(G(94.0m), G(0.5m), G(94.5m), Precision).Surplus.ShouldBeNull();
        ReadingClassifier.Classify(G(94.0m), G(0.5m), G(93.0m), Precision).Surplus.ShouldBeNull();
    }

    [Fact]
    public void Surplus_OnTheFloorScale_IsInKilograms() =>
        ReadingClassifier.Classify(Kg(24.000m), Kg(0.120m), Kg(24.300m), Floor).Surplus.ShouldBe(new Surplus(Kg(0.300m), 1.3m));
}
