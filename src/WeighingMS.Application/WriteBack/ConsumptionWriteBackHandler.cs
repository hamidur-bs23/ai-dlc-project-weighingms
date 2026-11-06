using WeighingMS.Application.Abstractions;
using WeighingMS.Application.Weighing;
using WeighingMS.Domain.WriteBack;

namespace WeighingMS.Application.WriteBack;

/// <summary>
/// Story 001 (PD-01): on <c>BatchCompleted</c>, adds the outbox entry to the completing unit of work, so it is saved in
/// the same transaction as the completion. It never calls OrgaTEX: completion is never blocked by the write-back.
/// </summary>
public sealed class ConsumptionWriteBackHandler(IWriteBackRepository writeBacks, IWriteBackSignal signal, IPlantClock clock) : IBatchCompletedHandler
{
    /// <inheritdoc />
    public Task HandleAsync(BatchCompletedNotice notice, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notice);
        var completed = notice.Completed;
        var lines = completed.Records
            .OrderBy(r => r.Sequence)
            .Select(r => new ConsumptionLine(r.ChemicalId, r.Actual.Value, r.Actual.Symbol, r.Lot))
            .ToList();
        var sessionId = completed.Records.Count > 0 ? completed.Records[0].SessionId : Guid.Empty;
        writeBacks.Add(ConsumptionWriteBack.Create(completed.BatchId, sessionId, lines, clock.UtcNow));
        signal.Notify(); // the dispatcher waits a moment, so it finds the row once the completion is committed
        return Task.CompletedTask;
    }
}
