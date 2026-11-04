using System.Diagnostics;
using ErrorOr;
using WeighingMS.Application.Abstractions;

namespace WeighingMS.Application.Devices;

/// <summary>
/// Device use cases: scales (list, status, classified reading, tare), scanner (scans, status) and the aggregated
/// health for the status strip. Every call updates the <see cref="DeviceHealthTracker"/>.
/// </summary>
public sealed class DeviceService(
    IScaleDevice scales,
    IScanner scanner,
    IEnumerable<IDeviceHealthProbe> probes,
    DeviceHealthTracker tracker,
    DevicesOptions options,
    TimeProvider clock)
{
    /// <summary>Scanner device name.</summary>
    public const string ScannerDevice = "scanner";

    /// <summary>The configured UI poll interval.</summary>
    public TimeSpan PollInterval => options.ScalePollInterval;

    /// <summary>All scales with their current status.</summary>
    public async Task<ErrorOr<IReadOnlyList<(ScaleInfo Scale, DeviceStatus Status)>>> ListScalesAsync(CancellationToken cancellationToken)
    {
        var list = await scales.ListAsync(cancellationToken);
        if (list.IsError)
        {
            return list.Errors;
        }

        var result = new List<(ScaleInfo, DeviceStatus)>();
        foreach (var scale in list.Value)
        {
            var status = await scales.GetStatusAsync(scale.ScaleId, cancellationToken);
            result.Add((scale, tracker.Observe(scale.ScaleId, status.IsError ? DeviceStatus.Offline : status.Value).Status));
        }

        return result;
    }

    /// <summary>Status of one scale and since when.</summary>
    public async Task<ErrorOr<(DeviceStatus Status, DateTimeOffset Since)>> GetScaleStatusAsync(string scaleId, CancellationToken cancellationToken)
    {
        var status = await scales.GetStatusAsync(scaleId, cancellationToken);
        if (status.IsError && status.FirstError.Type == ErrorType.NotFound)
        {
            return status.Errors;
        }

        return tracker.Observe(scaleId, status.IsError ? DeviceStatus.Offline : status.Value);
    }

    /// <summary>
    /// Reads and classifies the current weight. An unavailable scale is recorded as offline and returns
    /// <see cref="DeviceErrors.ScaleUnavailable"/>; a failed read is traced (healthy polls are not).
    /// </summary>
    public async Task<ErrorOr<ScaleReading>> ReadAsync(string scaleId, CancellationToken cancellationToken)
    {
        var started = clock.GetTimestamp();
        var raw = await scales.ReadAsync(scaleId, cancellationToken);
        WmsTelemetry.ScaleReadDuration.Record(
            clock.GetElapsedTime(started).TotalSeconds,
            new KeyValuePair<string, object?>("result", raw.IsError ? raw.FirstError.Code : "ok"));

        if (raw.IsError)
        {
            if (raw.FirstError.Type != ErrorType.NotFound)
            {
                tracker.Observe(scaleId, DeviceStatus.Offline);
                using var activity = WmsTelemetry.Source.StartActivity("wms.scale.read");
                activity?.SetTag("wms.scale.id", scaleId);
                activity?.SetStatus(ActivityStatusCode.Error, raw.FirstError.Code);
            }

            return raw.Errors;
        }

        var (_, since) = tracker.Observe(scaleId, DeviceStatus.Ready);
        var reading = raw.Value;
        var validity = ScaleReadingRules.Classify(reading, options.ZeroBands.GetValueOrDefault(scaleId));
        var net = reading.Net is { } value && reading.Unit is { } unit ? new DeviceQuantity(value, unit) : null;
        return new ScaleReading(reading.ScaleId, net, reading.Stable, validity, reading.ReadAt, since);
    }

    /// <summary>Tares a scale.</summary>
    public async Task<ErrorOr<TareResult>> TareAsync(string scaleId, CancellationToken cancellationToken)
    {
        var result = await scales.TareAsync(scaleId, cancellationToken);
        if (result.IsError && result.FirstError.Type != ErrorType.NotFound)
        {
            tracker.Observe(scaleId, DeviceStatus.Offline);
        }
        else if (!result.IsError)
        {
            tracker.Observe(scaleId, DeviceStatus.Ready);
        }

        return result;
    }

    /// <summary>Scans after the cursor, and the new cursor (the last sequence returned, or the given one).</summary>
    public async Task<ErrorOr<(IReadOnlyList<ScanEvent> Items, long Cursor)>> GetScansAsync(long after, CancellationToken cancellationToken)
    {
        if (after < 0)
        {
            return DeviceErrors.InvalidCursor;
        }

        var scans = await scanner.GetScansAfterAsync(after, cancellationToken);
        if (scans.IsError)
        {
            tracker.Observe(ScannerDevice, DeviceStatus.Offline);
            return scans.Errors;
        }

        tracker.Observe(ScannerDevice, DeviceStatus.Ready);
        var cursor = scans.Value.Count > 0 ? scans.Value[^1].Sequence : after;
        return (scans.Value, cursor);
    }

    /// <summary>Scanner status and since when.</summary>
    public async Task<(DeviceStatus Status, DateTimeOffset Since)> GetScannerStatusAsync(CancellationToken cancellationToken)
    {
        var status = await scanner.GetStatusAsync(cancellationToken);
        return tracker.Observe(ScannerDevice, status.IsError ? DeviceStatus.Offline : status.Value);
    }

    /// <summary>Health of every scale, the scanner and every probe (printer, OrgaTEX), for the status strip.</summary>
    public async Task<IReadOnlyList<DeviceHealth>> GetHealthAsync(CancellationToken cancellationToken)
    {
        var health = new List<DeviceHealth>();
        var list = await scales.ListAsync(cancellationToken);
        if (!list.IsError)
        {
            foreach (var scaleId in list.Value.Select(scale => scale.ScaleId))
            {
                var (status, since) = await GetScaleStatusAsync(scaleId, cancellationToken) is { IsError: false } s
                    ? s.Value
                    : tracker.Observe(scaleId, DeviceStatus.Offline);
                health.Add(new DeviceHealth(scaleId, DeviceKind.Scale, status, since));
            }
        }

        var (scannerStatus, scannerSince) = await GetScannerStatusAsync(cancellationToken);
        health.Add(new DeviceHealth(ScannerDevice, DeviceKind.Scanner, scannerStatus, scannerSince));

        foreach (var probe in probes)
        {
            var (status, since) = tracker.Observe(probe.Device, await probe.CheckAsync(cancellationToken));
            health.Add(new DeviceHealth(probe.Device, probe.Kind, status, since));
        }

        return health;
    }
}
