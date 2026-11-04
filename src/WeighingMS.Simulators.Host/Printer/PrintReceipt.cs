namespace WeighingMS.Simulators.Host.Printer;

/// <summary>What the dummy printer printed for one ticket.</summary>
public sealed record PrintReceipt(
    Guid PrintJobId,
    string BatchId,
    string IdempotencyKey,
    bool IsReprint,
    DateTimeOffset PrintedAt,
    IReadOnlyList<string> Lines,
    BarcodeDto? Barcode);
