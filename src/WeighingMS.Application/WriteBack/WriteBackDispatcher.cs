using WeighingMS.Application.Abstractions;
using WeighingMS.Application.Batches;
using WeighingMS.Domain.Audit;
using WeighingMS.Domain.WriteBack;

namespace WeighingMS.Application.WriteBack;

/// <summary>The outcome of one dispatch pass.</summary>
public sealed record DispatchResult(int Sent, int Retrying, int Failed);

/// <summary>
/// Story 002–003: posts due write-backs to OrgaTEX with their idempotency key, marks them sent on 200 OK, retries
/// transient failures with back-off, and marks them "sync failed" (audited) after the limit or a rejection.
/// </summary>
public sealed class WriteBackDispatcher(
    IWriteBackRepository writeBacks,
    IOrgatexGateway orgatex,
    WriteBackOptions options,
    IAuditTrail audit,
    IPlantClock clock)
{
    /// <summary>How many write-backs one pass handles.</summary>
    public const int BatchSize = 20;

    /// <summary>Posts every write-back that is due now.</summary>
    public async Task<DispatchResult> DispatchDueAsync(CancellationToken cancellationToken)
    {
        int sent = 0, retrying = 0, failed = 0;
        foreach (var writeBack in await writeBacks.ListDueAsync(clock.UtcNow, BatchSize, cancellationToken))
        {
            var posted = await orgatex.PostConsumptionAsync(writeBack.BatchId, writeBack.IdempotencyKey, writeBack.Lines, cancellationToken);
            var now = clock.UtcNow;
            if (!posted.IsError)
            {
                writeBack.RecordSent(posted.Value, now);
                sent++;
            }
            else if (writeBack.RecordFailure(ErrorCodeOf(posted.FirstError), OrgatexErrors.IsTransient(posted.FirstError), options.Policy, now))
            {
                audit.Append(AuditEntry.Record(
                    AuditEventType.SyncFailed, "system", now, writeBack.BatchId,
                    $"{writeBack.Attempts} attempt(s); last error {writeBack.LastErrorCode}"));
                failed++;
            }
            else
            {
                retrying++;
            }

            await writeBacks.SaveAsync(cancellationToken);
        }

        return new DispatchResult(sent, retrying, failed);
    }

    private static string ErrorCodeOf(ErrorOr.Error error) =>
        error.Metadata?.TryGetValue("orgatexCode", out var code) == true && code is string text ? $"{error.Code}:{text}" : error.Code;
}
