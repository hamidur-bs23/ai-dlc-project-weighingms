namespace WeighingMS.Api.Contracts.Devices;

/// <summary>A quantity on the wire (api-conventions: never a bare number).</summary>
public sealed record QuantityResponse(decimal Value, string Unit);
