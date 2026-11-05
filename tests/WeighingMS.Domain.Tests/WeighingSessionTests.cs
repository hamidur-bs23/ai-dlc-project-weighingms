using WeighingMS.Domain.Weighing;
using static WeighingMS.Domain.Tests.TestRecipes;

namespace WeighingMS.Domain.Tests;

/// <summary>Stories 001 (start/resume), 002 (select), 003 (scale per component), 007 (tolerance per component).</summary>
public sealed class WeighingSessionTests
{
    [Fact]
    public void Start_FromTheSnapshot_AllPendingFirstSuggestedNoneActive()
    {
        var session = Start();

        session.BatchId.ShouldBe("B-24187");
        session.Status.ShouldBe(WeighingSessionStatus.InProgress);
        session.StartedBy.ShouldBe("OP-1001");
        session.StartedAt.ShouldBe(Now);
        session.Id.Version.ShouldBe(7);
        session.Components.Select(c => c.Sequence).ShouldBe([1, 2, 3, 4, 5]);
        session.Components.ShouldAllBe(c => c.State == ComponentState.Pending);
        session.Suggested!.Sequence.ShouldBe(1);
        session.Active.ShouldBeNull();
        session.ActiveSequence.ShouldBeNull();
        session.DoneCount.ShouldBe(0);
        session.Events.ShouldHaveSingleItem().ShouldBe(new WeighingSessionStarted("B-24187", "OP-1001", 5, Now));
    }

    [Fact]
    public void Start_B24187_ChoosesTheScaleOfEachComponent()
    {
        var byCode = Start().Components.ToDictionary(c => c.Code);

        byCode["DYE-1180"].ScaleId.ShouldBe("scale-3kg");   // 1240.0 g
        byCode["DYE-4471"].ScaleId.ShouldBe("scale-3kg");   // Yellow RGB 94.0 g → 3 kg precision
        byCode["DYE-4471"].ScaleClass.ShouldBe(ScaleClass.Precision);
        byCode["AUX-0101"].ScaleId.ShouldBe("scale-32kg");  // Sodium Sulphate 24.000 kg → 32 kg floor
        byCode["AUX-0101"].ScaleClass.ShouldBe(ScaleClass.HeavyDuty);
        byCode["AUX-0107"].ScaleId.ShouldBe("scale-32kg");
    }

    [Fact]
    public void Start_KeepsTheScaleProfileAndTheBandPerComponent()
    {
        var yellow = Start().Components[2];

        yellow.Scale.ShouldBe(Precision);
        yellow.Target.ShouldBe(G(94.0m));
        yellow.SuggestedContainer.ShouldBe("C-12");
        yellow.Band!.Lower.ShouldBe(G(93.5m));
        yellow.Band.Upper.ShouldBe(G(94.5m));
    }

    [Fact]
    public void Start_RecipeTolerance_IsUsedWithItsSource()
    {
        var yellow = Start(configured: (_, _) => G(9m)).Components[2];

        yellow.Tolerance.PlusMinus.ShouldBe(G(0.5m));
        yellow.ToleranceSource.ShouldBe(ToleranceSource.Recipe);
    }

    [Fact]
    public void Start_NoRecipeTolerance_UsesTheConfiguredOneForChemicalAndSelectedClass()
    {
        (string, ScaleClass)? asked = null;
        var session = Start(Teal4C(sodaAshTolerance: null), (chemical, scaleClass) =>
        {
            asked = (chemical, scaleClass);
            return Kg(0.048m);
        });

        var soda = session.Components[4];
        asked.ShouldBe(("AUX-0107", ScaleClass.HeavyDuty));
        soda.ToleranceSource.ShouldBe(ToleranceSource.Configured);
        soda.Tolerance.PlusMinus.ShouldBe(Kg(0.048m));
    }

    [Fact]
    public void Start_NoToleranceAnywhere_TheComponentIsBlockedButTheOthersAreNot()
    {
        var session = Start(Teal4C(sodaAshTolerance: null));

        var soda = session.Components[4];
        soda.Tolerance.IsBlocked.ShouldBeTrue();
        soda.ToleranceSource.ShouldBeNull();
        soda.Band.ShouldBeNull();
        soda.Classify(Kg(9.600m)).ShouldBeNull();
        session.Components.Take(4).ShouldAllBe(c => !c.Tolerance.IsBlocked);
    }

    [Fact]
    public void Start_AssignedScaleMissingFromTheStation_IsRejected()
    {
        var onlyPrecision = new Dictionary<string, ScaleProfile> { ["scale-3kg"] = Precision };

        var result = WeighingSession.Start(Teal4C(), DefaultPolicy, onlyPrecision, (_, _) => null, "OP-1001", Now);

        result.FirstError.Code.ShouldBe("Weighing.ScaleNotAvailable");
        result.FirstError.NumericType.ShouldBe(503);
    }

    [Fact]
    public void Select_Pending_BecomesActiveOthersStayPending()
    {
        var session = Start();
        session.ClearEvents();

        session.Select(3, "OP-1001", Now).IsError.ShouldBeFalse();

        session.ActiveSequence.ShouldBe(3);
        session.Active!.Code.ShouldBe("DYE-4471");
        session.Components.Where(c => c.Sequence != 3).ShouldAllBe(c => c.State == ComponentState.Pending);
        session.Suggested!.Sequence.ShouldBe(1);   // the suggestion is the first pending, not the active one
        session.Events.ShouldHaveSingleItem().ShouldBe(new ComponentSelected("B-24187", "OP-1001", 3, "DYE-4471", "scale-3kg", ToleranceSource.Recipe, Now));
    }

    [Fact]
    public void Select_AnyOrder_IsAllowed()
    {
        var session = Start();

        session.Select(5, "OP-1001", Now).IsError.ShouldBeFalse();

        session.ActiveSequence.ShouldBe(5);
    }

    [Fact]
    public void Select_SwitchingComponent_AbandonsTheAttemptWithoutARecord()
    {
        var session = Start();
        session.Select(3, "OP-1001", Now);
        session.ClearEvents();

        session.Select(4, "OP-1002", Now.AddMinutes(1));

        session.Components[2].State.ShouldBe(ComponentState.Pending);
        session.Active!.Sequence.ShouldBe(4);
        session.DoneCount.ShouldBe(0);
        session.Events.Count.ShouldBe(2);
        session.Events[0].ShouldBe(new WeighingAttemptAbandoned("B-24187", "OP-1002", 3, Now.AddMinutes(1)));
        session.Events[1].ShouldBeOfType<ComponentSelected>().Sequence.ShouldBe(4);
    }

    [Fact]
    public void Select_TheActiveOneAgain_ChangesNothing()
    {
        var session = Start();
        session.Select(3, "OP-1001", Now);
        session.ClearEvents();

        session.Select(3, "OP-1001", Now).IsError.ShouldBeFalse();

        session.ActiveSequence.ShouldBe(3);
        session.Events.ShouldBeEmpty();
    }

    [Fact]
    public void Select_UnknownSequence_IsNotFound()
    {
        var session = Start();

        session.Select(9, "OP-1001", Now).FirstError.Code.ShouldBe("Weighing.ComponentNotFound");
        session.ActiveSequence.ShouldBeNull();
    }

    [Fact]
    public void Select_BlockedComponent_CanBeSelectedToSeeWhy()
    {
        var session = Start(Teal4C(sodaAshTolerance: null));

        session.Select(5, "OP-1001", Now).IsError.ShouldBeFalse();

        session.Active!.Tolerance.IsBlocked.ShouldBeTrue();
    }

    [Fact]
    public void Resume_RaisesResumedAndResetsNothing()
    {
        var session = Start();
        session.Select(2, "OP-1001", Now);
        session.ClearEvents();

        session.Resume("OP-1002", Now.AddHours(8));

        session.ActiveSequence.ShouldBe(2);
        session.StartedBy.ShouldBe("OP-1001");
        session.Events.ShouldHaveSingleItem().ShouldBe(new WeighingSessionResumed("B-24187", "OP-1002", 0, Now.AddHours(8)));
    }

    [Fact]
    public void ActiveComponent_ClassifiesReadingsOnItsOwnScale()
    {
        var session = Start();
        session.Select(4, "OP-1001", Now);

        var c = session.Active!.Classify(Kg(24.048m))!;

        c.Class.ShouldBe(WeightClass.WithinRange);
        c.Net.Unit.ShouldBe(WeighingMS.Domain.Common.MassUnit.Kilogram);
    }

    [Fact]
    public void Components_IsACopy()
    {
        var session = Start();

        var list = (IList<ComponentWeighing>)session.Components;
        list.ShouldNotBeSameAs(session.Components);
    }
}
