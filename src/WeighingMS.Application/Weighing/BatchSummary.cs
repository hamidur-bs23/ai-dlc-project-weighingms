using WeighingMS.Domain.Common;
using WeighingMS.Domain.Weighing;

namespace WeighingMS.Application.Weighing;

/// <summary>
/// The summary of a batch (FR-18): n of N, within tolerance (YES / NO + overrides), total dispensed (A13), elapsed (A14),
/// operators (A12), OrgaTEX and ticket state (A15: not yet available until bolts 011/012) and one line per chemical.
/// </summary>
public sealed record BatchSummary(
    string BatchId,
    string OrderId,
    string Machine,
    string Shade,
    WeighingSessionStatus Status,
    int Done,
    int Total,
    bool AllWithinTolerance,
    int OverrideCount,
    Quantity TotalDispensed,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    long? ElapsedSeconds,
    IReadOnlyList<string> Operators,
    string OrgatexState,
    string TicketState,
    IReadOnlyList<BatchSummaryLine> Lines);

/// <summary>One chemical of the summary: required, actual (when done) and final status.</summary>
public sealed record BatchSummaryLine(int Sequence, string ChemicalId, string ChemicalName, Quantity Required, Quantity? Actual, ComponentState State, WeightClass? Class, bool Override);

/// <summary>One completed batch in the review list (A18).</summary>
public sealed record CompletedBatchItem(string BatchId, string OrderId, string Machine, string Shade, DateTimeOffset CompletedAt, int Done, int Total, bool AllWithinTolerance, int OverrideCount);

/// <summary>A page of the review list.</summary>
public sealed record CompletedBatchPage(IReadOnlyList<CompletedBatchItem> Items, int Page, int PageSize, int Total);

/// <summary>Builds summaries from a session and its records (pure).</summary>
public static class BatchSummaryBuilder
{
    /// <summary>The state shown for OrgaTEX and the ticket until bolts 011 / 012 provide it (A15).</summary>
    public const string NotAvailable = "notAvailable";

    /// <summary>The summary; <paramref name="nameOf"/> turns a badge into the operator's display name.</summary>
    public static BatchSummary Build(WeighingSession session, string orderId, string machine, string shade, IReadOnlyDictionary<string, string> nameOf)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(nameOf);
        var records = session.Components.Where(c => c.Record is not null).Select(c => c.Record!).ToList();
        var overrides = records.Count(r => r.IsOverride);
        var totalKg = records.Sum(r => r.Actual.InKilograms);
        var operators = records.OrderBy(r => r.ConfirmedAt).Select(r => r.OperatorBadgeId).Distinct(StringComparer.Ordinal)
            .Select(b => nameOf.GetValueOrDefault(b, b)).ToList();
        long? elapsed = session.CompletedAt is { } done ? (long)(done - session.StartedAt).TotalSeconds : null;

        return new BatchSummary(
            session.BatchId,
            orderId,
            machine,
            shade,
            session.Status,
            session.DoneCount,
            session.Components.Count,
            overrides == 0 && records.All(r => r.Class is WeightClass.WithinRange or WeightClass.TargetAchieved),
            overrides,
            Quantity.Of(totalKg, MassUnit.Kilogram),
            session.StartedAt,
            session.CompletedAt,
            elapsed,
            operators,
            NotAvailable,
            NotAvailable,
            [.. session.Components.Select(c => new BatchSummaryLine(
                c.Sequence, c.ChemicalId, c.Name, c.Target, c.Record?.Actual, c.State, c.Record?.Class, c.Record?.IsOverride ?? false))]);
    }
}
