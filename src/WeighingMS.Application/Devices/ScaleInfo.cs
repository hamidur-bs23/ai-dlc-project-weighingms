namespace WeighingMS.Application.Devices;

/// <summary>A station scale as reported by the device.</summary>
public sealed record ScaleInfo(string ScaleId, string Name, string ScaleClass, DeviceQuantity Capacity, DeviceQuantity Resolution);
