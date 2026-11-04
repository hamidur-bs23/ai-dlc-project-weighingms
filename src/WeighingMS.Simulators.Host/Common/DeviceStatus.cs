namespace WeighingMS.Simulators.Host.Common;

/// <summary>Device status values used by the training contracts.</summary>
public static class DeviceStatus
{
    /// <summary>The device works.</summary>
    public const string Ready = "READY";

    /// <summary>The device is unavailable (an <c>unavailable</c> fault is active).</summary>
    public const string Offline = "OFFLINE";

    /// <summary>READY or OFFLINE.</summary>
    public static string From(bool unavailable) => unavailable ? Offline : Ready;
}
