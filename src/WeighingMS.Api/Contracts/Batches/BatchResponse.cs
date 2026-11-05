using WeighingMS.Api.Contracts.Devices;

namespace WeighingMS.Api.Contracts.Batches;

/// <summary>A batch in the queue (mockup view 1 columns) with its WeighingMS status.</summary>
public sealed record BatchResponse(
    string BatchId,
    string OrderId,
    string Machine,
    string Fabric,
    QuantityResponse BatchWeight,
    string Shade,
    int ComponentCount,
    string Status,
    bool TrainingOnly);
