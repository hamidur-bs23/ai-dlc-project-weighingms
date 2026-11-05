using WeighingMS.Domain.Weighing;

namespace WeighingMS.Application.Weighing;

/// <summary>
/// Reacts to a completed batch inside the completing transaction (COMPLETION-HANDLERS): whatever a handler adds to the
/// unit of work is saved atomically with the completion. Bolt 011 adds the OrgaTEX outbox row, bolt 012 the ticket.
/// </summary>
public interface IBatchCompletedHandler
{
    /// <summary>Handles the completion; must not save, the caller saves everything together.</summary>
    Task HandleAsync(BatchCompletedNotice notice, CancellationToken cancellationToken);
}

/// <summary>The completed batch: the event plus the batch header from the recipe snapshot.</summary>
public sealed record BatchCompletedNotice(BatchCompleted Completed, string OrderId, string Machine, string Shade);
