using Microsoft.Extensions.Time.Testing;
using WeighingMS.Application.Batches;
using WeighingMS.Application.Devices;
using WeighingMS.Application.Weighing;
using WeighingMS.Domain.Audit;
using WeighingMS.Domain.Common;
using WeighingMS.Domain.Weighing;

namespace WeighingMS.Application.Tests;

/// <summary>Bolt 008 use cases: capture, tare, confirm (server read), quit, leave, with fakes at every port.</summary>
public sealed class WeighingAttemptServiceTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 5, 1, 12, 31, TimeSpan.Zero));
    private readonly FakeSessions _sessions = new();
    private readonly FakeScales _scales = new();
    private readonly FakeAudit _audit = new();
    private readonly WeighingSessionService _service;

    public WeighingAttemptServiceTests()
    {
        var clock = new FakePlantClock(_time);
        var devices = new DeviceService(_scales, new NoScanner(), [], new DeviceHealthTracker(_time), new DevicesOptions(), _time);
        _service = new WeighingSessionService(_sessions, new BatchService(new FakeOrgatex(), new FakeSnapshots(), clock), new FakeTolerances(), _scales, devices, new FakeSelection(), _audit, clock);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private RawScaleReading Reading(decimal net, bool stable = true, string scaleId = "scale-3kg", string unit = "g") =>
        new(scaleId, net, unit, stable, false, _time.GetUtcNow());

    private async Task SelectedYellowAsync()
    {
        (await _service.StartOrResumeAsync("B-24187", "OP-1001", Ct)).IsError.ShouldBeFalse();
        (await _service.SelectAsync("B-24187", 3, "OP-1001", Ct)).IsError.ShouldBeFalse();
        _audit.Entries.Clear();
        _sessions.Saves = 0;
    }

    private async Task ReadyYellowAsync()
    {
        await SelectedYellowAsync();
        // The scale is observed online before the tare (as the Weigh screen's reading poll does).
        _scales.Reading = Reading(0m);
        (await _service.ReadCurrentAsync("B-24187", Ct)).IsError.ShouldBeFalse();
        _time.Advance(TimeSpan.FromSeconds(1));
        (await _service.CaptureLotAsync("B-24187", 3, " 2026-0387 ", "OP-1001", Ct)).IsError.ShouldBeFalse();
        (await _service.CaptureContainerAsync("B-24187", 3, "C-12", "OP-1001", Ct)).IsError.ShouldBeFalse();
        (await _service.TareAsync("B-24187", 3, "OP-1001", Ct)).IsError.ShouldBeFalse();
        _audit.Entries.Clear();
        _sessions.Saves = 0;
    }

    [Fact]
    public async Task CaptureLotAndContainer_AreStoredTrimmedAndNotAudited()
    {
        await SelectedYellowAsync();

        await _service.CaptureLotAsync("B-24187", 3, " 2026-0387 ", "OP-1001", Ct);
        var session = (await _service.CaptureContainerAsync("B-24187", 3, "C-12", "OP-1001", Ct)).Value;

        session.Active!.Attempt!.Lot.ShouldBe("2026-0387");
        session.Active.Attempt.Container.ShouldBe("C-12");
        _sessions.Saves.ShouldBe(2);
        _audit.Entries.ShouldBeEmpty(); // carried by the record instead
    }

    [Fact]
    public async Task CaptureLot_Invalid_IsAFieldErrorAndNotSaved()
    {
        await SelectedYellowAsync();

        var result = await _service.CaptureLotAsync("B-24187", 3, "", "OP-1001", Ct);

        result.FirstError.Code.ShouldBe("Request.Invalid");
        _sessions.Saves.ShouldBe(0);
    }

    [Fact]
    public async Task Capture_NoSession_IsNotFound() =>
        (await _service.CaptureLotAsync("B-24187", 3, "L", "OP-1001", Ct)).FirstError.Code.ShouldBe("Weighing.SessionNotFound");

    [Fact]
    public async Task Tare_TaresTheComponentsOwnScaleAndStoresTheReportedValue()
    {
        await SelectedYellowAsync();

        var session = (await _service.TareAsync("B-24187", 3, "OP-1001", Ct)).Value;

        _scales.TareCalls.ShouldBe(1);
        var tare = session.Active!.Attempt!.Tare!;
        tare.ScaleId.ShouldBe("scale-3kg");
        tare.Tare.ShouldBe(Quantity.Of(214.0m, MassUnit.Gram));
        tare.TakenAt.ShouldBe(_time.GetUtcNow()); // the Api's clock, the same clock that sets onlineSince
        _audit.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Tare_Again_IsAReTareAndAudited()
    {
        await ReadyYellowAsync();
        _scales.TareValue = 215.5m;

        var session = (await _service.TareAsync("B-24187", 3, "OP-1001", Ct)).Value;

        session.Active!.Attempt!.Tare!.Tare.Value.ShouldBe(215.5m);
        var entry = _audit.Entries.ShouldHaveSingleItem();
        entry.Type.ShouldBe(AuditEventType.ReTare);
        entry.Subject.ShouldBe("B-24187#3");
        entry.Reason.ShouldBe("tare 214.0 g → 215.5 g");
    }

    [Fact]
    public async Task Tare_ScaleDown_Is503AndNothingStored()
    {
        await SelectedYellowAsync();
        _scales.Down = true;

        (await _service.TareAsync("B-24187", 3, "OP-1001", Ct)).FirstError.Code.ShouldBe("Scale.Unavailable");
        _sessions.Saves.ShouldBe(0);
    }

    [Theory]
    [InlineData(4, "Weighing.ComponentNotActive")]
    [InlineData(9, "Weighing.ComponentNotFound")]
    public async Task Tare_NotTheActiveComponent_IsRejectedWithoutTouchingTheScale(int sequence, string code)
    {
        await SelectedYellowAsync();

        (await _service.TareAsync("B-24187", sequence, "OP-1001", Ct)).FirstError.Code.ShouldBe(code);
        _scales.TareCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Confirm_ReadsTheScaleOnTheServerAndRecordsThatValue()
    {
        await ReadyYellowAsync();
        _scales.Reading = Reading(94.2m);

        var (record, session) = (await _service.ConfirmAsync("B-24187", 3, "OP-1001", Ct)).Value;

        _scales.LastReadScale.ShouldBe("scale-3kg");
        record.Actual.ShouldBe(Quantity.Of(94.2m, MassUnit.Gram));
        record.Lot.ShouldBe("2026-0387");
        record.Tare.ShouldBe(Quantity.Of(214.0m, MassUnit.Gram));
        record.ConfirmedAt.ShouldBe(_time.GetUtcNow());
        session.ActiveSequence.ShouldBe(4);
        _sessions.Saves.ShouldBe(1);
        var entry = _audit.Entries.ShouldHaveSingleItem();
        entry.Type.ShouldBe(AuditEventType.WeighingConfirmed);
        entry.Subject.ShouldBe("B-24187#3");
    }

    [Fact]
    public async Task Confirm_Twice_SecondIsAlreadyConfirmedWithoutReadingTheScale()
    {
        await ReadyYellowAsync();
        _scales.Reading = Reading(94.2m);
        await _service.ConfirmAsync("B-24187", 3, "OP-1001", Ct);
        _scales.Reading = null; // a scale call would now fail

        (await _service.ConfirmAsync("B-24187", 3, "OP-1001", Ct)).FirstError.Code.ShouldBe("Weighing.AlreadyConfirmed");
    }

    [Theory]
    [InlineData(94.6, true, "Weighing.OutOfTolerance")]
    [InlineData(93.4, true, "Weighing.OutOfTolerance")]
    [InlineData(94.0, false, "Weighing.NotConfirmable")]
    public async Task Confirm_RuleNotMet_IsRejectedAndNothingStored(decimal net, bool stable, string code)
    {
        await ReadyYellowAsync();
        _scales.Reading = Reading(net, stable);

        (await _service.ConfirmAsync("B-24187", 3, "OP-1001", Ct)).FirstError.Code.ShouldBe(code);

        _sessions.Saves.ShouldBe(0);
        _audit.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Confirm_ScaleWentOfflineAfterTheTare_IsTareStale()
    {
        await ReadyYellowAsync();
        _scales.Down = true;
        (await _service.ReadCurrentAsync("B-24187", Ct)).IsError.ShouldBeTrue(); // the outage is observed
        _time.Advance(TimeSpan.FromSeconds(5));
        _scales.Down = false;
        _scales.Reading = Reading(94.2m);

        var result = await _service.ConfirmAsync("B-24187", 3, "OP-1001", Ct);

        result.Errors.Select(e => e.Metadata!["field"]).ShouldBe(["tareStale"]);
    }

    [Fact]
    public async Task Confirm_ScaleDown_Is503()
    {
        await ReadyYellowAsync();
        _scales.Down = true;

        (await _service.ConfirmAsync("B-24187", 3, "OP-1001", Ct)).FirstError.Code.ShouldBe("Scale.Unavailable");
    }

    [Fact]
    public async Task Confirm_ConcurrentChange_IsAConflict()
    {
        await ReadyYellowAsync();
        _scales.Reading = Reading(94.2m);
        _sessions.Conflict = true;

        (await _service.ConfirmAsync("B-24187", 3, "OP-1001", Ct)).FirstError.Code.ShouldBe("Concurrency.Conflict");
    }

    [Fact]
    public async Task Confirm_PendingComponent_IsNotActive()
    {
        await ReadyYellowAsync();

        (await _service.ConfirmAsync("B-24187", 4, "OP-1001", Ct)).FirstError.Code.ShouldBe("Weighing.ComponentNotActive");
    }

    [Fact]
    public async Task Confirm_NoSession_IsNotFound() =>
        (await _service.ConfirmAsync("B-24187", 3, "OP-1001", Ct)).FirstError.Code.ShouldBe("Weighing.SessionNotFound");

    [Fact]
    public async Task Reading_ReportsTheSameGapsAsConfirm()
    {
        await SelectedYellowAsync();
        _scales.Reading = Reading(94.2m);

        var reading = (await _service.ReadCurrentAsync("B-24187", Ct)).Value;

        reading.Check.CanConfirm.ShouldBeFalse();
        reading.Check.Missing.ShouldBe([ConfirmationGap.Lot, ConfirmationGap.Container, ConfirmationGap.Tare]);
        reading.Classification!.Class.ShouldBe(WeightClass.WithinRange);
    }

    [Fact]
    public async Task Reading_ReadyAndInTolerance_CanConfirm()
    {
        await ReadyYellowAsync();
        _scales.Reading = Reading(94.2m);

        (await _service.ReadCurrentAsync("B-24187", Ct)).Value.Check.CanConfirm.ShouldBeTrue();
    }

    [Fact]
    public async Task Quit_DiscardsAndAudits()
    {
        await ReadyYellowAsync();

        var session = (await _service.QuitAsync("B-24187", 3, "OP-1001", Ct)).Value;

        session.Active.ShouldBeNull();
        session.Components[2].State.ShouldBe(ComponentState.Pending);
        _audit.Entries.ShouldHaveSingleItem().Type.ShouldBe(AuditEventType.Quit);
    }

    [Fact]
    public async Task Leave_DiscardsTheAttemptAndAuditsBatchLeft()
    {
        await ReadyYellowAsync();

        var session = (await _service.LeaveAsync("B-24187", "OP-1001", Ct)).Value;

        session.Active.ShouldBeNull();
        var entry = _audit.Entries.ShouldHaveSingleItem();
        entry.Type.ShouldBe(AuditEventType.BatchLeft);
        entry.Reason.ShouldBe("0 of 5 done");
    }

    [Fact]
    public async Task Leave_NoSession_IsNotFound() =>
        (await _service.LeaveAsync("B-24187", "OP-1001", Ct)).FirstError.Code.ShouldBe("Weighing.SessionNotFound");

    [Theory]
    [InlineData(ReadingValidity.Valid, true, true)]
    [InlineData(ReadingValidity.BelowZero, true, false)]
    [InlineData(ReadingValidity.OverCapacity, false, false)]
    [InlineData(ReadingValidity.Malformed, false, false)]
    public void Measured_MapsValidityForClassificationAndConfirming(ReadingValidity validity, bool hasNet, bool valid)
    {
        var reading = new ScaleReading("scale-3kg", new DeviceQuantity(94.0m, "g"), true, validity, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);

        var measured = WeighingSessionService.Measured(reading);

        (measured.Net is not null).ShouldBe(hasNet);
        measured.Valid.ShouldBe(valid);
        measured.OnlineSince.ShouldBe(DateTimeOffset.UnixEpoch);
    }
}
