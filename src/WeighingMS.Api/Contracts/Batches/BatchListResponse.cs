namespace WeighingMS.Api.Contracts.Batches;

/// <summary>The batch queue (empty when OrgaTEX has released none).</summary>
public sealed record BatchListResponse(IReadOnlyList<BatchResponse> Items);
