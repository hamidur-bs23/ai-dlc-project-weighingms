using System.Collections.Concurrent;

namespace WeighingMS.Application.Devices;

/// <summary>
/// Remembers each device's last observed status and when it changed (the "since" time shown in the status strip,
/// and the scale's <c>onlineSince</c>). Singleton; thread-safe.
/// </summary>
public sealed class DeviceHealthTracker(TimeProvider clock)
{
    private readonly ConcurrentDictionary<string, (DeviceStatus Status, DateTimeOffset Since)> _state = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Records an observation; returns the status and since when it has held.</summary>
    public (DeviceStatus Status, DateTimeOffset Since) Observe(string device, DeviceStatus status)
    {
        var now = clock.GetUtcNow();
        return _state.AddOrUpdate(
            device,
            _ => (status, now),
            (_, previous) => previous.Status == status ? previous : (status, now));
    }

    /// <summary>The last observation, if any.</summary>
    public (DeviceStatus Status, DateTimeOffset Since)? Last(string device) =>
        _state.TryGetValue(device, out var value) ? value : null;
}
