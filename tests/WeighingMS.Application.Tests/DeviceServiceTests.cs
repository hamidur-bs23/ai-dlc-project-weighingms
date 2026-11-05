using ErrorOr;
using Microsoft.Extensions.Time.Testing;
using WeighingMS.Application.Devices;

namespace WeighingMS.Application.Tests;

public sealed class DeviceServiceTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 3, 21, 48, 22, TimeSpan.Zero));
    private readonly FakeScales _scales = new();
    private readonly FakeScanner _scanner = new();
    private readonly FakeProbe _printer = new("printer", DeviceKind.Printer);
    private readonly DeviceService _service;

    public DeviceServiceTests()
    {
        var options = new DevicesOptions { ZeroBands = new(StringComparer.OrdinalIgnoreCase) { ["scale-3kg"] = 0.5m } };
        _service = new DeviceService(_scales, _scanner, [_printer], new DeviceHealthTracker(_time), options, _time);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Read_ValidReading_IsClassifiedWithOnlineSince()
    {
        _scales.Reading = new RawScaleReading("scale-3kg", 94.2m, "g", true, false, _time.GetUtcNow());

        var reading = await _service.ReadAsync("scale-3kg", Ct);

        reading.Value.Net.ShouldBe(new DeviceQuantity(94.2m, "g"));
        reading.Value.Validity.ShouldBe(ReadingValidity.Valid);
        reading.Value.OnlineSince.ShouldBe(_time.GetUtcNow());
    }

    [Fact]
    public async Task Read_AfterAnOutage_OnlineSinceMovesForward()
    {
        _scales.Reading = new RawScaleReading("scale-3kg", 0m, "g", true, false, _time.GetUtcNow());
        var before = (await _service.ReadAsync("scale-3kg", Ct)).Value.OnlineSince;

        _scales.Unavailable = true;
        _time.Advance(TimeSpan.FromSeconds(5));
        (await _service.ReadAsync("scale-3kg", Ct)).FirstError.Code.ShouldBe("Scale.Unavailable");
        _scales.Unavailable = false;
        _time.Advance(TimeSpan.FromSeconds(5));

        (await _service.ReadAsync("scale-3kg", Ct)).Value.OnlineSince.ShouldBe(before!.Value.AddSeconds(10));
    }

    [Fact]
    public async Task Read_BelowZeroBand_IsBelowZero()
    {
        _scales.Reading = new RawScaleReading("scale-3kg", -0.6m, "g", true, false, _time.GetUtcNow());

        (await _service.ReadAsync("scale-3kg", Ct)).Value.Validity.ShouldBe(ReadingValidity.BelowZero);
    }

    [Fact]
    public async Task Read_MalformedReading_HasNoNet()
    {
        _scales.Reading = new RawScaleReading("scale-3kg", null, null, false, false, _time.GetUtcNow());

        var reading = (await _service.ReadAsync("scale-3kg", Ct)).Value;

        reading.Net.ShouldBeNull();
        reading.Validity.ShouldBe(ReadingValidity.Malformed);
    }

    [Fact]
    public async Task Read_UnknownScale_IsNotFoundAndNotRecordedOffline()
    {
        _scales.NotFound = true;

        (await _service.ReadAsync("scale-x", Ct)).FirstError.Type.ShouldBe(ErrorType.NotFound);
        (await _service.GetHealthAsync(Ct)).ShouldNotContain(h => h.Device == "scale-x");
    }

    [Fact]
    public async Task Scans_ReturnsCursorOfLastScanOrTheGivenCursor()
    {
        _scanner.Scans.Add(new ScanEvent(1, "B-24187", _time.GetUtcNow()));
        _scanner.Scans.Add(new ScanEvent(2, "2026-0387", _time.GetUtcNow()));

        (await _service.GetScansAsync(0, Ct)).Value.Cursor.ShouldBe(2);
        (await _service.GetScansAsync(2, Ct)).Value.Cursor.ShouldBe(2);
        (await _service.GetScansAsync(-1, Ct)).FirstError.Code.ShouldBe("Request.Invalid");
    }

    [Fact]
    public async Task Health_ListsScalesScannerAndProbes()
    {
        _scanner.Unavailable = true;
        _printer.Status = DeviceStatus.Offline;

        var health = await _service.GetHealthAsync(Ct);

        health.Select(h => (h.Device, h.Kind, h.Status)).ShouldBe(
        [
            ("scale-3kg", DeviceKind.Scale, DeviceStatus.Ready),
            ("scanner", DeviceKind.Scanner, DeviceStatus.Offline),
            ("printer", DeviceKind.Printer, DeviceStatus.Offline),
        ]);
    }

    [Fact]
    public async Task Tare_Unavailable_RecordsOffline()
    {
        _scales.Unavailable = true;

        (await _service.TareAsync("scale-3kg", Ct)).FirstError.Code.ShouldBe("Scale.Unavailable");
        (await _service.GetScaleStatusAsync("scale-3kg", Ct)).Value.Status.ShouldBe(DeviceStatus.Offline);
    }

    private sealed class FakeScales : IScaleDevice
    {
        public RawScaleReading? Reading { get; set; }

        public bool Unavailable { get; set; }

        public bool NotFound { get; set; }

        public Task<ErrorOr<IReadOnlyList<ScaleInfo>>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<ErrorOr<IReadOnlyList<ScaleInfo>>>(new List<ScaleInfo>
            {
                new("scale-3kg", "3 kg precision", "precision", new DeviceQuantity(3200m, "g"), new DeviceQuantity(0.1m, "g")),
            });

        public Task<ErrorOr<DeviceStatus>> GetStatusAsync(string scaleId, CancellationToken cancellationToken) =>
            Task.FromResult<ErrorOr<DeviceStatus>>(Unavailable ? DeviceErrors.ScaleUnavailable(scaleId) : DeviceStatus.Ready);

        public Task<ErrorOr<RawScaleReading>> ReadAsync(string scaleId, CancellationToken cancellationToken)
        {
            if (NotFound)
            {
                return Task.FromResult<ErrorOr<RawScaleReading>>(DeviceErrors.ScaleNotFound(scaleId));
            }

            return Task.FromResult<ErrorOr<RawScaleReading>>(Unavailable ? DeviceErrors.ScaleUnavailable(scaleId) : Reading!);
        }

        public Task<ErrorOr<TareResult>> TareAsync(string scaleId, CancellationToken cancellationToken) =>
            Task.FromResult<ErrorOr<TareResult>>(Unavailable
                ? DeviceErrors.ScaleUnavailable(scaleId)
                : new TareResult(scaleId, new DeviceQuantity(214m, "g"), DateTimeOffset.UnixEpoch));
    }

    private sealed class FakeScanner : IScanner
    {
        public List<ScanEvent> Scans { get; } = [];

        public bool Unavailable { get; set; }

        public Task<ErrorOr<IReadOnlyList<ScanEvent>>> GetScansAfterAsync(long after, CancellationToken cancellationToken) =>
            Task.FromResult<ErrorOr<IReadOnlyList<ScanEvent>>>(Unavailable ? DeviceErrors.ScannerUnavailable : Scans.Where(s => s.Sequence > after).ToList());

        public Task<ErrorOr<DeviceStatus>> GetStatusAsync(CancellationToken cancellationToken) =>
            Task.FromResult<ErrorOr<DeviceStatus>>(Unavailable ? DeviceErrors.ScannerUnavailable : DeviceStatus.Ready);
    }

    private sealed class FakeProbe(string device, DeviceKind kind) : IDeviceHealthProbe
    {
        public DeviceStatus Status { get; set; } = DeviceStatus.Ready;

        public string Device => device;

        public DeviceKind Kind => kind;

        public Task<DeviceStatus> CheckAsync(CancellationToken cancellationToken) => Task.FromResult(Status);
    }
}
