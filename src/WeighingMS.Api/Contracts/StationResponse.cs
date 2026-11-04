namespace WeighingMS.Api.Contracts;

/// <summary>Station identity and the plant clock (UTC and plant local time with offset).</summary>
public sealed record StationResponse(string StationId, string TimeZone, DateTimeOffset UtcNow, DateTimeOffset PlantNow);
