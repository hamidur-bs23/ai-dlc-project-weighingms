namespace WeighingMS.Api.Contracts.Weighing;

/// <summary>A batch's ticket: the same content as the printed ticket, for the on-screen preview (FR-21).</summary>
public sealed record TicketResponse(
    string BatchId,
    string State,
    int PrintCount,
    IReadOnlyList<string> Lines,
    TicketBarcodeResponse Barcode,
    DateTimeOffset? FirstPrintedAt,
    DateTimeOffset? LastPrintedAt);

/// <summary>The barcode: Code 128 of the batch ID, with the value printed beneath it.</summary>
public sealed record TicketBarcodeResponse(string Symbology, string Value);

/// <summary>
/// Print the ticket. <c>expectedPrintCount</c> is the print count the screen last showed: a stale press is refused, so a
/// double press can't become a surprise REPRINT.
/// </summary>
public sealed record PrintTicketRequest(int? ExpectedPrintCount);

/// <summary>The ticket on the summary: <c>notAvailable</c> (no ticket) · <c>printPending</c> · <c>printed</c>.</summary>
public sealed record TicketStateResponse(string State, int PrintCount)
{
    /// <summary>No ticket: the batch isn't complete, or completed before tickets existed.</summary>
    public static TicketStateResponse NotAvailable { get; } = new("notAvailable", 0);
}
