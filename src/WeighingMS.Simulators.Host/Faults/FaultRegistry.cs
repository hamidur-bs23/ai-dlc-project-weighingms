using System.Collections.Concurrent;

namespace WeighingMS.Simulators.Host.Faults;

/// <summary>
/// The active faults per target. A fault with a duration expires by itself (timed by <see cref="TimeProvider"/>);
/// a new fault on a target replaces the previous one.
/// </summary>
public sealed class FaultRegistry(TimeProvider clock)
{
    private readonly ConcurrentDictionary<string, ActiveFaultResponse> _faults = new(StringComparer.Ordinal);

    /// <summary>Sets or replaces the fault on a target.</summary>
    public ActiveFaultResponse Set(string target, FaultMode mode, TimeSpan? duration)
    {
        var key = FaultTargets.Normalise(target);
        var fault = new ActiveFaultResponse(key, mode, duration is null ? null : clock.GetUtcNow() + duration.Value);
        _faults[key] = fault;
        return fault;
    }

    /// <summary>The active fault mode of a target, or null. Expired faults are removed.</summary>
    public FaultMode? Get(string target)
    {
        var key = FaultTargets.Normalise(target);
        if (!_faults.TryGetValue(key, out var fault))
        {
            return null;
        }

        if (fault.ExpiresAt is { } expiresAt && clock.GetUtcNow() >= expiresAt)
        {
            _faults.TryRemove(key, out _);
            return null;
        }

        return fault.Mode;
    }

    /// <summary>True while the target has an <see cref="FaultMode.Unavailable"/> fault (status reports OFFLINE).</summary>
    public bool IsUnavailable(string target) => Get(target) == FaultMode.Unavailable;

    /// <summary>All active, unexpired faults.</summary>
    public IReadOnlyList<ActiveFaultResponse> All() =>
        [.. _faults.Keys.Where(k => Get(k) is not null).Select(k => _faults[k]).OrderBy(f => f.Target, StringComparer.Ordinal)];

    /// <summary>Clears every fault.</summary>
    public void Clear() => _faults.Clear();
}
