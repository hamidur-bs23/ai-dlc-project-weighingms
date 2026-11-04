namespace WeighingMS.Application.Devices;

/// <summary>The tare value the scale stored.</summary>
public sealed record TareResult(string ScaleId, DeviceQuantity Tare, DateTimeOffset TaredAt);
