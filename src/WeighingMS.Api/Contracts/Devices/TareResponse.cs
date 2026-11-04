namespace WeighingMS.Api.Contracts.Devices;

/// <summary>The tare the scale stored.</summary>
public sealed record TareResponse(string ScaleId, QuantityResponse Tare, DateTimeOffset TaredAt);
