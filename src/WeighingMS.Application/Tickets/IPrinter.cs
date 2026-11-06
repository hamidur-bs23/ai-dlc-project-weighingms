using ErrorOr;

namespace WeighingMS.Application.Tickets;

/// <summary>What is sent to the printer for one print.</summary>
public sealed record TicketPrint(string BatchId, bool IsReprint, Guid IdempotencyKey, IReadOnlyList<string> Lines, string BarcodeSymbology, string BarcodeValue);

/// <summary>The ticket printer port (the dummy printer in the lab; the real model is unconfirmed, Q18, A-04).</summary>
public interface IPrinter
{
    /// <summary>
    /// Prints a ticket. A repeated key prints nothing and returns the first result. Returns when it was printed;
    /// <see cref="PrinterErrors.Unavailable"/> when the printer doesn't accept it.
    /// </summary>
    Task<ErrorOr<DateTimeOffset>> PrintAsync(TicketPrint print, CancellationToken cancellationToken);
}

/// <summary>Printer failures.</summary>
public static class PrinterErrors
{
    /// <summary>The printer is offline, timing out or failing (503): the ticket stays as it was.</summary>
    public static readonly Error Unavailable = Error.Custom(503, "Printer.Unavailable", "The printer is not responding.");
}
