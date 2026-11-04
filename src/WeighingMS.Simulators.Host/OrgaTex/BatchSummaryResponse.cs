namespace WeighingMS.Simulators.Host.OrgaTex;

/// <summary>A queue row as released by the dummy OrgaTEX (training contract).</summary>
public sealed record BatchSummaryResponse(
    string BatchId,
    string OrderId,
    string Machine,
    string Fabric,
    decimal BatchWeightKg,
    string Shade,
    int ComponentCount,
    string Status,
    bool TrainingOnly);
