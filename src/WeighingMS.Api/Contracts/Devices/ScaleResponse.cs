namespace WeighingMS.Api.Contracts.Devices;

/// <summary>A station scale.</summary>
public sealed record ScaleResponse(string ScaleId, string Name, string ScaleClass, QuantityResponse Capacity, QuantityResponse Resolution, string Status);
