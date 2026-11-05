using Microsoft.Extensions.Time.Testing;
using WeighingMS.Application.Devices;

namespace WeighingMS.Application.Tests;

public sealed class DeviceHealthTrackerTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 3, 21, 48, 22, TimeSpan.Zero));

    [Fact]
    public void Observe_FirstTime_SinceIsNow()
    {
        var tracker = new DeviceHealthTracker(_time);

        tracker.Observe("scale-3kg", DeviceStatus.Ready).Since.ShouldBe(_time.GetUtcNow());
    }

    [Fact]
    public void Observe_SameStatusLater_KeepsTheOriginalSince()
    {
        var tracker = new DeviceHealthTracker(_time);
        var first = tracker.Observe("scale-3kg", DeviceStatus.Ready).Since;

        _time.Advance(TimeSpan.FromMinutes(3));

        tracker.Observe("scale-3kg", DeviceStatus.Ready).Since.ShouldBe(first);
    }

    [Fact]
    public void Observe_StatusChange_MovesSinceToTheChange()
    {
        var tracker = new DeviceHealthTracker(_time);
        tracker.Observe("scale-3kg", DeviceStatus.Ready);
        _time.Advance(TimeSpan.FromSeconds(10));
        var offlineAt = _time.GetUtcNow();
        tracker.Observe("scale-3kg", DeviceStatus.Offline).Since.ShouldBe(offlineAt);
        _time.Advance(TimeSpan.FromSeconds(20));

        var back = tracker.Observe("scale-3kg", DeviceStatus.Ready);

        back.Status.ShouldBe(DeviceStatus.Ready);
        back.Since.ShouldBe(offlineAt.AddSeconds(20));
        tracker.Last("scale-3kg").ShouldBe((DeviceStatus.Ready, offlineAt.AddSeconds(20)));
    }

    [Fact]
    public void Last_UnknownDevice_IsNull()
    {
        new DeviceHealthTracker(_time).Last("printer").ShouldBeNull();
    }
}
