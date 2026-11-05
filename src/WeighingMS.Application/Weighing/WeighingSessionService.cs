using ErrorOr;
using WeighingMS.Application.Abstractions;
using WeighingMS.Application.Batches;
using WeighingMS.Application.Devices;
using WeighingMS.Domain.Audit;
using WeighingMS.Domain.Common;
using WeighingMS.Domain.Weighing;

namespace WeighingMS.Application.Weighing;

/// <summary>
/// Weighing-session use cases (bolt 007): start or resume, read, select a component, classify the current reading.
/// Every rule is decided by the Domain; this service only gathers its inputs and stores the result.
/// </summary>
public sealed class WeighingSessionService(
    IWeighingSessionRepository sessions,
    BatchService batches,
    IToleranceCatalogue tolerances,
    IScaleDevice scales,
    DeviceService devices,
    IScaleSelectionSource scaleSelection,
    IAuditTrail audit,
    IPlantClock clock)
{
    /// <summary>
    /// Resumes the batch's session, or starts one from the recipe snapshot (decision SESSION-START). Starting reads the
    /// scale list once (SCALE-PROFILE) and the current configuration (A1); resuming needs neither OrgaTEX nor a scale.
    /// </summary>
    public async Task<ErrorOr<(WeighingSession Session, bool Started)>> StartOrResumeAsync(string batchId, string operatorBadgeId, CancellationToken cancellationToken)
    {
        if (await sessions.FindAsync(batchId, cancellationToken) is { } existing)
        {
            return await ResumeAsync(existing, operatorBadgeId, cancellationToken);
        }

        var recipe = await batches.GetRecipeAsync(batchId, cancellationToken);
        if (recipe.IsError)
        {
            return recipe.Errors;
        }

        var profiles = await ReadScaleProfilesAsync(cancellationToken);
        if (profiles.IsError)
        {
            return profiles.Errors;
        }

        var policy = scaleSelection.Current();
        if (policy.IsError)
        {
            return policy.Errors;
        }

        var started = WeighingSession.Start(
            recipe.Value,
            policy.Value,
            profiles.Value,
            (chemicalId, scaleClass) => tolerances.Find(chemicalId, WireScaleClass(scaleClass)),
            operatorBadgeId,
            clock.UtcNow);
        if (started.IsError)
        {
            return started.Errors;
        }

        AppendAudit(started.Value);
        var stored = await sessions.AddOrGetExistingAsync(started.Value, cancellationToken);
        if (!stored.Started)
        {
            // Another request started the same batch first: this open is a resume of that session.
            return await ResumeAsync(stored.Session, operatorBadgeId, cancellationToken);
        }

        stored.Session.ClearEvents();
        return (stored.Session, true);
    }

    /// <summary>The batch's session.</summary>
    public async Task<ErrorOr<WeighingSession>> GetAsync(string batchId, CancellationToken cancellationToken) =>
        await sessions.FindAsync(batchId, cancellationToken) is { } session ? session : WeighingErrors.SessionNotFound(batchId);

    /// <summary>Makes a pending component active (FR-4); switching abandons the current attempt.</summary>
    public async Task<ErrorOr<WeighingSession>> SelectAsync(string batchId, int sequence, string operatorBadgeId, CancellationToken cancellationToken)
    {
        if (await sessions.FindAsync(batchId, cancellationToken) is not { } session)
        {
            return WeighingErrors.SessionNotFound(batchId);
        }

        var selected = session.Select(sequence, operatorBadgeId, clock.UtcNow);
        if (selected.IsError)
        {
            return selected.Errors;
        }

        if (session.Events.Count == 0)
        {
            return session; // already active: nothing changed
        }

        AppendAudit(session);
        var saved = await sessions.SaveAsync(cancellationToken);
        if (saved.IsError)
        {
            return saved.Errors;
        }

        session.ClearEvents();
        return session;
    }

    /// <summary>
    /// Reads the active component's own scale and classifies the reading (WEIGH-READING). A read: nothing is stored.
    /// </summary>
    public async Task<ErrorOr<CurrentReading>> ReadCurrentAsync(string batchId, CancellationToken cancellationToken)
    {
        if (await sessions.FindAsync(batchId, cancellationToken) is not { } session)
        {
            return WeighingErrors.SessionNotFound(batchId);
        }

        if (session.Active is not { } component)
        {
            return WeighingErrors.NoActiveComponent;
        }

        var reading = await devices.ReadAsync(component.ScaleId, cancellationToken);
        if (reading.IsError)
        {
            return reading.Errors;
        }

        var r = reading.Value;
        Classification? classification = null;
        if (r.Validity is ReadingValidity.Valid or ReadingValidity.BelowZero
            && r.Net is { } net
            && Quantity.Create(net.Value, net.Unit) is { IsError: false } quantity)
        {
            classification = component.Classify(quantity.Value);
        }

        return new CurrentReading(component, r, classification);
    }

    /// <summary>The configuration's wire name for a scale class (as used by the tolerance catalogue).</summary>
    public static string WireScaleClass(ScaleClass scaleClass) => scaleClass == ScaleClass.Precision ? "precision" : "heavyDuty";

    private async Task<ErrorOr<(WeighingSession Session, bool Started)>> ResumeAsync(WeighingSession session, string operatorBadgeId, CancellationToken cancellationToken)
    {
        session.Resume(operatorBadgeId, clock.UtcNow);
        AppendAudit(session);
        var saved = await sessions.SaveAsync(cancellationToken);
        if (saved.IsError)
        {
            return saved.Errors;
        }

        session.ClearEvents();
        return (session, false);
    }

    private async Task<ErrorOr<IReadOnlyDictionary<string, ScaleProfile>>> ReadScaleProfilesAsync(CancellationToken cancellationToken)
    {
        var list = await scales.ListAsync(cancellationToken);
        if (list.IsError)
        {
            return list.Errors;
        }

        var profiles = new Dictionary<string, ScaleProfile>(StringComparer.Ordinal);
        foreach (var scale in list.Value)
        {
            var resolution = Quantity.CreatePositive(scale.Resolution.Value, scale.Resolution.Unit);
            if (!resolution.IsError)
            {
                profiles[scale.ScaleId] = new ScaleProfile(scale.ScaleId, resolution.Value.Unit, resolution.Value);
            }
        }

        return profiles;
    }

    private void AppendAudit(WeighingSession session)
    {
        foreach (var e in session.Events)
        {
            audit.Append(e switch
            {
                WeighingSessionStarted s => AuditEntry.Record(AuditEventType.WeighingSessionStarted, s.OperatorBadgeId, s.OccurredAt, s.BatchId),
                WeighingSessionResumed r => AuditEntry.Record(AuditEventType.WeighingSessionResumed, r.OperatorBadgeId, r.OccurredAt, r.BatchId),
                ComponentSelected c => AuditEntry.Record(AuditEventType.ComponentSelected, c.OperatorBadgeId, c.OccurredAt, $"{c.BatchId}#{c.Sequence}"),
                WeighingAttemptAbandoned a => AuditEntry.Record(AuditEventType.AttemptAbandoned, a.OperatorBadgeId, a.OccurredAt, $"{a.BatchId}#{a.Sequence}"),
                _ => throw new InvalidOperationException($"Unmapped weighing event {e.GetType().Name}."),
            });
        }
    }
}
