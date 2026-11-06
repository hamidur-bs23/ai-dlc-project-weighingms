using Microsoft.Extensions.Time.Testing;
using WeighingMS.Application.Batches;
using WeighingMS.Application.Devices;
using WeighingMS.Application.Weighing;
using WeighingMS.Domain.Access;
using WeighingMS.Domain.Audit;
using WeighingMS.Domain.Common;
using WeighingMS.Domain.Weighing;

namespace WeighingMS.Application.Tests;

/// <summary>Bolt 009 use cases: override (grant flow), completion with handlers, summary, review list.</summary>
public sealed class WeighingOutcomeServiceTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 5, 1, 41, 41, TimeSpan.Zero));
    private readonly FakeSessions _sessions = new();
    private readonly FakeScales _scales = new();
    private readonly FakeAudit _audit = new();
    private readonly FakeGrants _grants = new();
    private readonly FakeUsers _users = new();
    private readonly RecordingHandler _handler = new();
    private readonly WeighingSessionService _service;

    public WeighingOutcomeServiceTests()
    {
        var clock = new FakePlantClock(_time);
        var devices = new DeviceService(_scales, new NoScanner(), [], new DeviceHealthTracker(_time), new DevicesOptions(), _time);
        var snapshots = new FakeSnapshots();
        _users.Users.Add(StationUser.CreateOperator("OP-1001", "R. Karim"));
        _users.Users.Add(StationUser.CreateOperator("OP-1002", "S. Akter"));
        _handler.Sessions = _sessions;
        _service = new WeighingSessionService(_sessions, new BatchService(new FakeOrgatex(), snapshots, clock, new FakeWriteBacks(), new FakeTickets()), new FakeTolerances(), _scales, devices, new FakeSelection(), _audit, clock, _grants, snapshots, _users, [_handler], new FakeWriteBacks(), new FakeTickets());
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private StepUpGrant NewGrant(string action = "override", string badge = "SV-2001")
    {
        var grant = StepUpGrant.Issue(StationUser.CreateSupervisor(badge, "M. Rahman", "hash"), action, _time.GetUtcNow(), TimeSpan.FromMinutes(2));
        _grants.Add(grant);
        return grant;
    }

    private RawScaleReading Reading(decimal net, string scaleId = "scale-3kg", string unit = "g") => new(scaleId, net, unit, true, false, _time.GetUtcNow());

    /// <summary>Starts B-24187 and readies component <paramref name="sequence"/> (lot, container, tare).</summary>
    private async Task ReadyAsync(int sequence, string op = "OP-1001")
    {
        if ((await _service.GetAsync("B-24187", Ct)).IsError)
        {
            (await _service.StartOrResumeAsync("B-24187", op, Ct)).IsError.ShouldBeFalse();
        }

        var session = (await _service.GetAsync("B-24187", Ct)).Value;
        if (session.ActiveSequence != sequence)
        {
            (await _service.SelectAsync("B-24187", sequence, op, Ct)).IsError.ShouldBeFalse();
        }

        var scaleId = session.Components.Single(c => c.Sequence == sequence).ScaleId;
        _scales.Reading = Reading(0m, scaleId, scaleId == "scale-3kg" ? "g" : "kg");
        await _service.ReadCurrentAsync("B-24187", Ct); // the scale is seen online before the tare
        _time.Advance(TimeSpan.FromSeconds(1));
        await _service.CaptureLotAsync("B-24187", sequence, $"L{sequence}", op, Ct);
        await _service.CaptureContainerAsync("B-24187", sequence, $"C{sequence}", op, Ct);
        _scales.TareValue = 0m;
        (await _service.TareAsync("B-24187", sequence, op, Ct)).IsError.ShouldBeFalse();
        _audit.Entries.Clear();
        _sessions.Saves = 0;
    }

    private async Task ConfirmAsync(int sequence, decimal net, string op = "OP-1001")
    {
        await ReadyAsync(sequence, op);
        var scaleId = sequence <= 3 ? "scale-3kg" : "scale-32kg";
        _scales.Reading = Reading(net, scaleId, sequence <= 3 ? "g" : "kg");
        _time.Advance(TimeSpan.FromSeconds(80));
        (await _service.ConfirmAsync("B-24187", sequence, op, Ct)).IsError.ShouldBeFalse();
    }

    [Fact]
    public async Task Override_Overweight_RecordsOvrAndConsumesTheGrantInTheSameSave()
    {
        await ReadyAsync(3);
        var grant = NewGrant();
        _scales.Reading = Reading(96.4m);

        var (record, session) = (await _service.OverrideAsync("B-24187", 3, grant.Id, "Shade lab accepts", "OP-1001", Ct)).Value;

        record.Class.ShouldBe(WeightClass.Overweight);
        record.Surplus!.Amount.ShouldBe(Quantity.Of(2.4m, MassUnit.Gram));
        record.Override!.SupervisorBadgeId.ShouldBe("SV-2001");
        record.Override.SupervisorName.ShouldBe("M. Rahman");
        record.Override.GrantId.ShouldBe(grant.Id);
        grant.UsedAt.ShouldBe(_time.GetUtcNow());
        _sessions.Saves.ShouldBe(1);
        session.ActiveSequence.ShouldBe(4);
        var entry = _audit.Entries.ShouldHaveSingleItem();
        entry.Type.ShouldBe(AuditEventType.Override);
        entry.Subject.ShouldBe("B-24187#3");
        entry.Reason.ShouldBe("Overweight 96.4 g authorised by SV-2001: Shade lab accepts");
    }

    [Theory]
    [InlineData("", "Request.Invalid")]       // no reason
    [InlineData("ok", "Weighing.OverrideNotNeeded")] // in range (94.2 g below)
    public async Task Override_FixableRejection_LeavesTheGrantUnused(string reason, string code)
    {
        await ReadyAsync(3);
        var grant = NewGrant();
        _scales.Reading = Reading(code == "Weighing.OverrideNotNeeded" ? 94.2m : 96.4m);

        (await _service.OverrideAsync("B-24187", 3, grant.Id, reason, "OP-1001", Ct)).FirstError.Code.ShouldBe(code);

        grant.UsedAt.ShouldBeNull();
        _sessions.Saves.ShouldBe(0);
    }

    [Fact]
    public async Task Override_SupervisorIsTheOperator_IsFourEyesAndTheGrantStaysUnused()
    {
        await ReadyAsync(3, "SV-2001");
        var grant = NewGrant();
        _scales.Reading = Reading(96.4m);

        (await _service.OverrideAsync("B-24187", 3, grant.Id, "self", "SV-2001", Ct)).FirstError.Code.ShouldBe("Auth.FourEyesRequired");
        grant.UsedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Override_UnknownUsedExpiredOrOtherActionGrant_IsGrantInvalidWithoutReadingTheScale()
    {
        await ReadyAsync(3);
        var used = NewGrant();
        used.Consume("override", _time.GetUtcNow());
        var otherAction = NewGrant("reprint");
        var expired = NewGrant();
        _scales.Reading = null; // any scale read would fail with 503

        (await _service.OverrideAsync("B-24187", 3, Guid.CreateVersion7(), "x", "OP-1001", Ct)).FirstError.Code.ShouldBe("Auth.GrantInvalid");
        (await _service.OverrideAsync("B-24187", 3, used.Id, "x", "OP-1001", Ct)).FirstError.Code.ShouldBe("Auth.GrantInvalid");
        (await _service.OverrideAsync("B-24187", 3, otherAction.Id, "x", "OP-1001", Ct)).FirstError.Code.ShouldBe("Auth.GrantInvalid");
        _time.Advance(TimeSpan.FromMinutes(3));
        (await _service.OverrideAsync("B-24187", 3, expired.Id, "x", "OP-1001", Ct)).FirstError.Code.ShouldBe("Auth.GrantInvalid");
    }

    [Fact]
    public async Task Override_NotActiveComponent_IsRejectedBeforeTheGrantIsLookedAt()
    {
        await ReadyAsync(3);

        (await _service.OverrideAsync("B-24187", 4, Guid.CreateVersion7(), "x", "OP-1001", Ct)).FirstError.Code.ShouldBe("Weighing.ComponentNotActive");
    }

    [Fact]
    public async Task Override_NoSession_IsNotFound() =>
        (await _service.OverrideAsync("B-24187", 3, Guid.CreateVersion7(), "x", "OP-1001", Ct)).FirstError.Code.ShouldBe("Weighing.SessionNotFound");

    [Fact]
    public async Task LastConfirm_CompletesAndCallsTheHandlersBeforeTheSave()
    {
        foreach (var (seq, net) in new[] { (1, 1241.5m), (2, 385.4m), (3, 94.2m), (4, 23.980m) })
        {
            await ConfirmAsync(seq, net);
        }

        _handler.Notices.ShouldBeEmpty();
        await ConfirmAsync(5, 9.621m, "OP-1002");

        var notice = _handler.Notices.ShouldHaveSingleItem();
        notice.Completed.BatchId.ShouldBe("B-24187");
        notice.Completed.Records.Count.ShouldBe(5);
        notice.OrderId.ShouldBe("SO-9931");
        notice.Machine.ShouldBe("JET-04");
        notice.Shade.ShouldBe("Teal 4C");
        _handler.SavesSeenAtCall.ShouldBe([0]); // called before the (single) save
        _audit.Entries.Select(e => e.Type).ShouldContain(AuditEventType.BatchCompleted);
    }

    [Fact]
    public async Task Summary_OfTheMockupBatch_MatchesViewThree()
    {
        foreach (var (seq, net) in new[] { (1, 1241.5m), (2, 385.4m), (3, 94.2m), (4, 23.980m) })
        {
            await ConfirmAsync(seq, net);
        }

        await ConfirmAsync(5, 9.621m, "OP-1002");

        var s = (await _service.GetSummaryAsync("B-24187", Ct)).Value;

        s.Status.ShouldBe(WeighingSessionStatus.Completed);
        (s.Done, s.Total).ShouldBe((5, 5));
        s.AllWithinTolerance.ShouldBeTrue();
        s.OverrideCount.ShouldBe(0);
        s.TotalDispensed.ShouldBe(Quantity.Of(35.3221m, MassUnit.Kilogram)); // shown as 35.322 kg
        s.Operators.ShouldBe(["R. Karim", "S. Akter"]);
        s.ElapsedSeconds.ShouldBe((long)(s.CompletedAt!.Value - s.StartedAt).TotalSeconds);
        s.Orgatex.ShouldBeNull(); // no write-back handler in this unit test
        s.Ticket.ShouldBeNull(); // no ticket handler in this unit test
        s.Lines.Select(l => l.Actual!.Value).ShouldBe([1241.5m, 385.4m, 94.2m, 23.980m, 9.621m]);
        (s.OrderId, s.Machine, s.Shade).ShouldBe(("SO-9931", "JET-04", "Teal 4C"));
    }

    [Fact]
    public async Task Summary_WithAnOverride_IsNotAllWithinToleranceAndCountsIt()
    {
        await ReadyAsync(3);
        var grant = NewGrant();
        _scales.Reading = Reading(96.4m);
        await _service.OverrideAsync("B-24187", 3, grant.Id, "ok", "OP-1001", Ct);

        var s = (await _service.GetSummaryAsync("B-24187", Ct)).Value;

        s.AllWithinTolerance.ShouldBeFalse();
        s.OverrideCount.ShouldBe(1);
        s.Status.ShouldBe(WeighingSessionStatus.InProgress);
        s.CompletedAt.ShouldBeNull();
        s.ElapsedSeconds.ShouldBeNull();
        var line = s.Lines.Single(l => l.Sequence == 3);
        (line.Class, line.Override).ShouldBe((WeightClass.Overweight, true));
        s.Lines.Single(l => l.Sequence == 1).State.ShouldBe(ComponentState.Pending);
    }

    [Fact]
    public async Task Complete_WhilePending_IsIncomplete()
    {
        await ConfirmAsync(1, 1240.0m);

        (await _service.CompleteAsync("B-24187", "OP-1001", Ct)).FirstError.Code.ShouldBe("Batch.Incomplete");
        _handler.Notices.ShouldBeEmpty();
    }

    [Fact]
    public async Task Complete_AfterAutoCompletion_ReturnsTheSummaryWithoutSavingAgain()
    {
        foreach (var (seq, net) in new[] { (1, 1240.0m), (2, 386.0m), (3, 94.0m), (4, 24.000m), (5, 9.600m) })
        {
            await ConfirmAsync(seq, net);
        }

        _sessions.Saves = 0;

        var summary = (await _service.CompleteAsync("B-24187", "OP-1001", Ct)).Value;

        summary.Status.ShouldBe(WeighingSessionStatus.Completed);
        _sessions.Saves.ShouldBe(0);
        _handler.Notices.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ListCompleted_IsNewestFirstPagedBy20()
    {
        var page = await _service.ListCompletedAsync(1, Ct);

        page.Items.ShouldBeEmpty();
        page.PageSize.ShouldBe(20);
        (await _service.ListCompletedAsync(0, Ct)).Page.ShouldBe(1);

        foreach (var (seq, net) in new[] { (1, 1240.0m), (2, 386.0m), (3, 94.0m), (4, 24.000m), (5, 9.600m) })
        {
            await ConfirmAsync(seq, net);
        }

        var completed = await _service.ListCompletedAsync(1, Ct);
        completed.Total.ShouldBe(1);
        var item = completed.Items.ShouldHaveSingleItem();
        (item.BatchId, item.OrderId, item.Done, item.Total, item.AllWithinTolerance).ShouldBe(("B-24187", "SO-9931", 5, 5, true));
    }

    private sealed class RecordingHandler : IBatchCompletedHandler
    {
        public List<BatchCompletedNotice> Notices { get; } = [];

        public List<int> SavesSeenAtCall { get; } = [];

        public FakeSessions? Sessions { get; set; }

        public Task HandleAsync(BatchCompletedNotice notice, CancellationToken cancellationToken)
        {
            Notices.Add(notice);
            SavesSeenAtCall.Add(Sessions?.Saves ?? 0);
            return Task.CompletedTask;
        }
    }
}
