namespace WeighingMS.Application.Devices;

/// <summary>A lightweight availability check for one device or integration shown in the status strip.</summary>
public interface IDeviceHealthProbe
{
    /// <summary>Device name shown in the strip, for example <c>printer</c>.</summary>
    string Device { get; }

    /// <summary>Kind of device.</summary>
    DeviceKind Kind { get; }

    /// <summary>Checks availability; never throws.</summary>
    Task<DeviceStatus> CheckAsync(CancellationToken cancellationToken);
}
