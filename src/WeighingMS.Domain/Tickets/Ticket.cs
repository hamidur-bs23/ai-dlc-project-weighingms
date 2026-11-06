using System.Security.Cryptography;
using ErrorOr;

namespace WeighingMS.Domain.Tickets;

/// <summary>
/// The weighing ticket of one completed batch (FR-21). Composed once, in the completing transaction, and frozen.
/// It stays print pending until the operator prints it (PD-02, TICKET-PRINT-MANUAL); every later print is a reprint.
/// </summary>
public sealed class Ticket
{
    /// <summary>The barcode symbology (PD-04).</summary>
    public const string BarcodeSymbology = "Code128";

    private List<string> _lines = [];

    private Ticket()
    {
        BatchId = string.Empty;
        BarcodeValue = string.Empty;
    }

    /// <summary>Identifier (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>The batch (one ticket per batch).</summary>
    public string BatchId { get; private set; }

    /// <summary>The composed lines, without any REPRINT mark.</summary>
    public IReadOnlyList<string> Lines => _lines;

    /// <summary>What the barcode encodes: the batch ID (PD-04).</summary>
    public string BarcodeValue { get; private set; }

    /// <summary>Status.</summary>
    public TicketStatus Status { get; private set; }

    /// <summary>How many prints the printer has accepted.</summary>
    public int PrintCount { get; private set; }

    /// <summary>The idempotency key of the first print; never regenerated.</summary>
    public Guid FirstPrintKey { get; private set; }

    /// <summary>When the ticket was composed (the batch's completion).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When it was first printed.</summary>
    public DateTimeOffset? FirstPrintedAt { get; private set; }

    /// <summary>When it was last printed.</summary>
    public DateTimeOffset? LastPrintedAt { get; private set; }

    /// <summary>Whether the next print is a reprint.</summary>
    public bool NextPrintIsReprint => PrintCount > 0;

    /// <summary>The lines of the next print: marked REPRINT after the first.</summary>
    public IReadOnlyList<string> NextPrintLines => NextPrintIsReprint ? TicketComposer.MarkReprint(_lines) : _lines;

    /// <summary>
    /// The idempotency key of the next print: fixed per print number, so concurrent presses and a retry after a lost
    /// response all carry the same key and the printer prints that copy once.
    /// </summary>
    public Guid NextPrintKey
    {
        get
        {
            if (PrintCount == 0)
            {
                return FirstPrintKey;
            }

            var input = new byte[20];
            FirstPrintKey.TryWriteBytes(input);
            BitConverter.TryWriteBytes(input.AsSpan(16), PrintCount);
            return new Guid(SHA256.HashData(input).AsSpan(0, 16));
        }
    }

    /// <summary>Creates a print-pending ticket.</summary>
    public static Ticket Create(string batchId, IReadOnlyCollection<string> lines, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchId);
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count == 0)
        {
            throw new ArgumentException("A ticket needs at least one line.", nameof(lines));
        }

        return new Ticket
        {
            Id = Guid.CreateVersion7(),
            BatchId = batchId,
            BarcodeValue = batchId,
            Status = TicketStatus.PrintPending,
            FirstPrintKey = Guid.CreateVersion7(),
            CreatedAt = now.ToUniversalTime(),
            _lines = [.. lines],
        };
    }

    /// <summary>
    /// Refuses a press made against an out-of-date print count, so a double press can't become a surprise REPRINT.
    /// </summary>
    public ErrorOr<Success> CheckPress(int expectedPrintCount) =>
        expectedPrintCount == PrintCount
            ? Result.Success
            : Error.Conflict("Ticket.AlreadyPrinted", $"The ticket of '{BatchId}' has been printed {PrintCount} time(s). Check before printing again.");

    /// <summary>The printer accepted a print.</summary>
    public void RecordPrinted(DateTimeOffset now)
    {
        var at = now.ToUniversalTime();
        PrintCount++;
        Status = TicketStatus.Printed;
        FirstPrintedAt ??= at;
        LastPrintedAt = at;
    }
}
