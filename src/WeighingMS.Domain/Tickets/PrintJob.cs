namespace WeighingMS.Domain.Tickets;

/// <summary>One attempt to print a ticket, successful or not: who, when, which key, and the outcome.</summary>
public sealed class PrintJob
{
    private PrintJob()
    {
        BatchId = string.Empty;
        OperatorBadgeId = string.Empty;
    }

    /// <summary>Identifier (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>The ticket.</summary>
    public Guid TicketId { get; private set; }

    /// <summary>The batch.</summary>
    public string BatchId { get; private set; }

    /// <summary>Whether this was a reprint.</summary>
    public bool IsReprint { get; private set; }

    /// <summary>The <c>Idempotency-Key</c> it was sent with.</summary>
    public Guid IdempotencyKey { get; private set; }

    /// <summary>Who pressed Print.</summary>
    public string OperatorBadgeId { get; private set; }

    /// <summary>The error code when the printer didn't accept it; null when printed.</summary>
    public string? ErrorCode { get; private set; }

    /// <summary>When it was attempted.</summary>
    public DateTimeOffset AttemptedAt { get; private set; }

    /// <summary>Records one attempt for the ticket's next print.</summary>
    public static PrintJob Record(Ticket ticket, Guid idempotencyKey, string operatorBadgeId, string? errorCode, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentException.ThrowIfNullOrWhiteSpace(operatorBadgeId);
        return new PrintJob
        {
            Id = Guid.CreateVersion7(),
            TicketId = ticket.Id,
            BatchId = ticket.BatchId,
            IsReprint = ticket.NextPrintIsReprint,
            IdempotencyKey = idempotencyKey,
            OperatorBadgeId = operatorBadgeId,
            ErrorCode = errorCode,
            AttemptedAt = now.ToUniversalTime(),
        };
    }
}
