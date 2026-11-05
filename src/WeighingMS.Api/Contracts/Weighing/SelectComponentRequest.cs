namespace WeighingMS.Api.Contracts.Weighing;

/// <summary>Select a pending component by its recipe sequence.</summary>
public sealed record SelectComponentRequest(int? Sequence);
