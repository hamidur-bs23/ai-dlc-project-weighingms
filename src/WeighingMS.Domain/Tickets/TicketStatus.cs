namespace WeighingMS.Domain.Tickets;

/// <summary>Where a ticket is in printing (PD-02).</summary>
public enum TicketStatus
{
    /// <summary>Composed, not printed yet: the operator prints it with F1 (no auto-print).</summary>
    PrintPending,

    /// <summary>Printed at least once.</summary>
    Printed,
}
