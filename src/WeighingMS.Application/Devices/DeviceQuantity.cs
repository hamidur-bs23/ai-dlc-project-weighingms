namespace WeighingMS.Application.Devices;

/// <summary>A device quantity: fixed-point value with its unit (g or kg).</summary>
public sealed record DeviceQuantity(decimal Value, string Unit);
