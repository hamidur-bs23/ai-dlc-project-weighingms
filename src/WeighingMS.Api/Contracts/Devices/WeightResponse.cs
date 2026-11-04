namespace WeighingMS.Api.Contracts.Devices;

/// <summary>
/// The live net weight. <c>validity</c> other than <c>valid</c> means the reading can never be confirmed;
/// <c>onlineSince</c> is when the scale last came back from offline.
/// </summary>
public sealed record WeightResponse(string ScaleId, QuantityResponse? Net, bool Stable, string Validity, DateTimeOffset ReadAt, DateTimeOffset? OnlineSince);
