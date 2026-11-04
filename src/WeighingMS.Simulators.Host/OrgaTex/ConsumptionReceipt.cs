namespace WeighingMS.Simulators.Host.OrgaTex;

/// <summary>What the dummy OrgaTEX stored for one consumption post.</summary>
public sealed record ConsumptionReceipt(
    string BatchId,
    string IdempotencyKey,
    DateTimeOffset ReceivedAt,
    IReadOnlyList<ConsumptionLineRequest> Lines);
