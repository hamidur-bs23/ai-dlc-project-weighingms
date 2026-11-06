using WeighingMS.Api.Contracts.Devices;

namespace WeighingMS.Api.Contracts.Weighing;

/// <summary>The batch summary (FR-18), view 3 "Confirm &amp; print"; read-only.</summary>
public sealed record BatchSummaryResponse(
    string BatchId,
    string OrderId,
    string Machine,
    string Shade,
    string Status,
    int Done,
    int Total,
    bool AllWithinTolerance,
    int OverrideCount,
    QuantityResponse TotalDispensed,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    long? ElapsedSeconds,
    IReadOnlyList<string> Operators,
    OrgatexStateResponse Orgatex,
    TicketStateResponse Ticket,
    IReadOnlyList<BatchSummaryLineResponse> Lines);

/// <summary>
/// The OrgaTEX write-back (bolt 011): <c>notAvailable</c> (batch not complete) · <c>pending</c> (with attempts and the next
/// attempt) · <c>sent</c> (with <c>writtenAt</c>) · <c>syncFailed</c> (with the error code; Retry offered).
/// </summary>
public sealed record OrgatexStateResponse(string State, DateTimeOffset? WrittenAt, int Attempts, DateTimeOffset? NextAttemptAt, string? ErrorCode)
{
    /// <summary>No write-back yet.</summary>
    public static OrgatexStateResponse NotAvailable { get; } = new("notAvailable", null, 0, null, null);
}

/// <summary>One chemical: required, actual and final status (<c>pending | active | withinRange | targetAchieved | underweight | overweight</c>).</summary>
public sealed record BatchSummaryLineResponse(int Sequence, string ChemicalId, string ChemicalName, QuantityResponse Required, QuantityResponse? Actual, string Status, bool Override);

/// <summary>A completed batch in the review list.</summary>
public sealed record CompletedBatchResponse(string BatchId, string OrderId, string Machine, string Shade, DateTimeOffset CompletedAt, int Done, int Total, bool AllWithinTolerance, int OverrideCount);

/// <summary>A page of completed batches, newest first.</summary>
public sealed record CompletedBatchPageResponse(IReadOnlyList<CompletedBatchResponse> Items, int Page, int PageSize, int Total);
