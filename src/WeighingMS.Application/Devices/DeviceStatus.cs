namespace WeighingMS.Application.Devices;

/// <summary>Availability of a device or integration.</summary>
public enum DeviceStatus
{
    /// <summary>Working.</summary>
    Ready,

    /// <summary>Unavailable (offline, timing out or failing).</summary>
    Offline,
}
