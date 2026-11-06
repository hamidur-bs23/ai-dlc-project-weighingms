using ErrorOr;
using WeighingMS.Application.Abstractions;
using WeighingMS.Domain.Audit;
using WeighingMS.Domain.Tickets;

namespace WeighingMS.Application.Tickets;

/// <summary>What the screen shows about a batch's ticket; the same content as the printed ticket (FR-21).</summary>
public sealed record TicketView(
    string BatchId,
    TicketStatus Status,
    int PrintCount,
    IReadOnlyList<string> Lines,
    string BarcodeSymbology,
    string BarcodeValue,
    DateTimeOffset? FirstPrintedAt,
    DateTimeOffset? LastPrintedAt);

/// <summary>Reads a ticket and prints it (stories 001–004). Printing is always an operator's press (TICKET-PRINT-MANUAL).</summary>
public sealed class TicketService(ITicketRepository tickets, IPrinter printer, IAuditTrail audit, IPlantClock clock)
{
    private static readonly Error _notFound = Error.NotFound("Ticket.NotFound", "This batch has no ticket.");

    /// <summary>The batch's ticket.</summary>
    public async Task<ErrorOr<TicketView>> GetAsync(string batchId, CancellationToken cancellationToken) =>
        await tickets.FindAsync(batchId, cancellationToken) is { } ticket ? View(ticket) : _notFound;

    /// <summary>
    /// Prints the ticket: the first print, or a reprint marked REPRINT and audited as one. If the printer is unavailable
    /// the ticket stays as it was and the press can be repeated. A press made against an old print count is refused.
    /// </summary>
    public async Task<ErrorOr<TicketView>> PrintAsync(string batchId, int expectedPrintCount, string operatorBadgeId, CancellationToken cancellationToken)
    {
        if (await tickets.FindAsync(batchId, cancellationToken) is not { } ticket)
        {
            return _notFound;
        }

        var press = ticket.CheckPress(expectedPrintCount);
        if (press.IsError)
        {
            return press.Errors;
        }

        var key = ticket.NextPrintKey;
        var isReprint = ticket.NextPrintIsReprint;
        var printed = await printer.PrintAsync(
            new TicketPrint(batchId, isReprint, key, ticket.NextPrintLines, Ticket.BarcodeSymbology, ticket.BarcodeValue),
            cancellationToken);

        var now = clock.UtcNow;
        tickets.AddJob(PrintJob.Record(ticket, key, operatorBadgeId, printed.IsError ? printed.FirstError.Code : null, now));
        if (printed.IsError)
        {
            await tickets.SaveAsync(cancellationToken);
            return printed.Errors;
        }

        ticket.RecordPrinted(now);
        audit.Append(AuditEntry.Record(
            isReprint ? AuditEventType.TicketReprinted : AuditEventType.TicketPrinted,
            operatorBadgeId,
            now,
            batchId,
            $"print {ticket.PrintCount}"));

        // A concurrent press carried the same key, so the printer printed once; the other press recorded it.
        return await tickets.SaveAsync(cancellationToken)
            ? View(ticket)
            : Error.Conflict("Ticket.AlreadyPrinted", $"The ticket of '{batchId}' was printed by another press.");
    }

    /// <summary>The view of a ticket.</summary>
    public static TicketView View(Ticket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        return new TicketView(ticket.BatchId, ticket.Status, ticket.PrintCount, ticket.Lines, Ticket.BarcodeSymbology, ticket.BarcodeValue, ticket.FirstPrintedAt, ticket.LastPrintedAt);
    }
}
