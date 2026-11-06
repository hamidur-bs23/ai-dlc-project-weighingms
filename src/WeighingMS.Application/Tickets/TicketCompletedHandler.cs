using WeighingMS.Application.Abstractions;
using WeighingMS.Application.Access;
using WeighingMS.Application.Weighing;
using WeighingMS.Domain.Tickets;

namespace WeighingMS.Application.Tickets;

/// <summary>
/// Story 001: on <c>BatchCompleted</c>, composes the ticket and adds it to the completing unit of work. It never calls the
/// printer, so a printer fault can't block or roll back a completion (PD-02).
/// </summary>
public sealed class TicketCompletedHandler(ITicketRepository tickets, IStationUserRepository users, IPlantClock clock) : IBatchCompletedHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(BatchCompletedNotice notice, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notice);
        var completed = notice.Completed;

        // TICKET-OPERATORS: everyone who weighed, in the order they first confirmed.
        var operators = new List<string>();
        foreach (var badge in completed.Records.OrderBy(r => r.ConfirmedAt).Select(r => r.OperatorBadgeId).Distinct(StringComparer.Ordinal))
        {
            var user = await users.FindByBadgeAsync(badge, cancellationToken);
            operators.Add(user?.DisplayName ?? badge);
        }

        var lines = TicketComposer.Compose(new TicketContent(
            completed.BatchId,
            notice.OrderId,
            notice.Machine,
            notice.Shade,
            clock.ToPlantTime(completed.OccurredAt),
            operators,
            [.. completed.Records.OrderBy(r => r.Sequence).Select(r => new TicketComponent(r.ChemicalName, r.Required, r.Actual, r.IsOverride))]));

        tickets.Add(Ticket.Create(completed.BatchId, lines, clock.UtcNow));
    }
}
