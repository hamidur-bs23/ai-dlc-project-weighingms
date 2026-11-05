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
    IntegrationStateResponse Orgatex,
    IntegrationStateResponse Ticket,
    IReadOnlyList<BatchSummaryLineResponse> Lines);

/// <summary>OrgaTEX write-back or ticket state (A15: <c>notAvailable</c> until bolts 011 / 012).</summary>
public sealed record IntegrationStateResponse(string State);

/// <summary>One chemical: required, actual and final status (<c>pending | active | withinRange | targetAchieved | underweight | overweight</c>).</summary>
public sealed record BatchSummaryLineResponse(int Sequence, string ChemicalId, string ChemicalName, QuantityResponse Required, QuantityResponse? Actual, string Status, bool Override);

/// <summary>A completed batch in the review list.</summary>
public sealed record CompletedBatchResponse(string BatchId, string OrderId, string Machine, string Shade, DateTimeOffset CompletedAt, int Done, int Total, bool AllWithinTolerance, int OverrideCount);

/// <summary>A page of completed batches, newest first.</summary>
public sealed record CompletedBatchPageResponse(IReadOnlyList<CompletedBatchResponse> Items, int Page, int PageSize, int Total);
