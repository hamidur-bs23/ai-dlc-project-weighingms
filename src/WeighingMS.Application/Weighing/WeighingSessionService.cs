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

        return await SaveAsync(session, cancellationToken);
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

        return new CurrentReading(component, reading.Value, ConfirmationRules.Check(component, Measured(reading.Value)));
    }

    /// <summary>Captures the lot for the active component (story 004).</summary>
    public Task<ErrorOr<WeighingSession>> CaptureLotAsync(string batchId, int sequence, string? lot, string operatorBadgeId, CancellationToken cancellationToken) =>
        ChangeAsync(batchId, s => s.SetLot(sequence, lot, operatorBadgeId, clock.UtcNow), cancellationToken);

    /// <summary>Captures the container for the active component (story 004).</summary>
    public Task<ErrorOr<WeighingSession>> CaptureContainerAsync(string batchId, int sequence, string? container, string operatorBadgeId, CancellationToken cancellationToken) =>
        ChangeAsync(batchId, s => s.SetContainer(sequence, container, operatorBadgeId, clock.UtcNow), cancellationToken);

    /// <summary>
    /// Tares the active component's own scale and stores the reported value (SESSION-TARE); a repeat is a re-tare.
    /// </summary>
    public async Task<ErrorOr<WeighingSession>> TareAsync(string batchId, int sequence, string operatorBadgeId, CancellationToken cancellationToken)
    {
        if (await sessions.FindAsync(batchId, cancellationToken) is not { } session)
        {
            return WeighingErrors.SessionNotFound(batchId);
        }

        var component = session.Components.SingleOrDefault(c => c.Sequence == sequence);
        if (component is null)
        {
            return WeighingErrors.ComponentNotFound(sequence);
        }

        if (component.State != ComponentState.Active)
        {
            return component.State == ComponentState.Done ? WeighingErrors.AlreadyConfirmed(sequence) : WeighingErrors.ComponentNotActive(sequence);
        }

        var tared = await devices.TareAsync(component.ScaleId, cancellationToken);
        if (tared.IsError)
        {
            return tared.Errors;
        }

        var tare = Quantity.Create(tared.Value.Tare.Value, tared.Value.Tare.Unit);
        if (tare.IsError)
        {
            return DeviceErrors.ScaleUnavailable(component.ScaleId);
        }

        var recorded = session.RecordTare(sequence, new TareTaken(component.ScaleId, tare.Value, clock.UtcNow), operatorBadgeId);
        return recorded.IsError ? recorded.Errors : await SaveAsync(session, cancellationToken);
    }

    /// <summary>
    /// Confirms the named component (story 008): the server reads the component's scale itself (CONFIRM-SERVER-READ),
    /// re-checks the confirm rule and stores exactly one record with the session change and audit in one transaction.
    /// </summary>
    public async Task<ErrorOr<(WeighingRecord Record, WeighingSession Session)>> ConfirmAsync(string batchId, int sequence, string operatorBadgeId, CancellationToken cancellationToken)
    {
        if (await sessions.FindAsync(batchId, cancellationToken) is not { } session)
        {
            return WeighingErrors.SessionNotFound(batchId);
        }

        var component = session.Components.SingleOrDefault(c => c.Sequence == sequence);
        if (component is null)
        {
            return WeighingErrors.ComponentNotFound(sequence);
        }

        if (component.State != ComponentState.Active)
        {
            return component.State == ComponentState.Done ? WeighingErrors.AlreadyConfirmed(sequence) : WeighingErrors.ComponentNotActive(sequence);
        }

        var reading = await devices.ReadAsync(component.ScaleId, cancellationToken);
        if (reading.IsError)
        {
            return reading.Errors;
        }

        var record = session.Confirm(sequence, Measured(reading.Value), operatorBadgeId, clock.UtcNow);
        if (record.IsError)
        {
            return record.Errors;
        }

        var saved = await SaveAsync(session, cancellationToken);
        if (saved.IsError)
        {
            return saved.Errors;
        }

        return (record.Value, session);
    }

    /// <summary>Quit (F4): discards the active attempt; no record (story 012).</summary>
    public Task<ErrorOr<WeighingSession>> QuitAsync(string batchId, int sequence, string operatorBadgeId, CancellationToken cancellationToken) =>
        ChangeAsync(batchId, s => s.Quit(sequence, operatorBadgeId, clock.UtcNow), cancellationToken);

    /// <summary>Leaves the batch (LEAVE-DISCARDS): the active attempt is discarded, records stay (story 012).</summary>
    public Task<ErrorOr<WeighingSession>> LeaveAsync(string batchId, string operatorBadgeId, CancellationToken cancellationToken) =>
        ChangeAsync(
            batchId,
            s =>
            {
                s.Leave(operatorBadgeId, clock.UtcNow);
                return Result.Success;
            },
            cancellationToken);

    /// <summary>The domain's view of a device reading (A10: only <see cref="ReadingValidity.Valid"/> is confirmable).</summary>
    public static MeasuredReading Measured(ScaleReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        Quantity? net = null;
        if (reading.Validity is ReadingValidity.Valid or ReadingValidity.BelowZero
            && reading.Net is { } value
            && Quantity.Create(value.Value, value.Unit) is { IsError: false } quantity)
        {
            net = quantity.Value;
        }

        return new MeasuredReading(net, reading.Stable, reading.Validity == ReadingValidity.Valid, reading.OnlineSince);
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

    private async Task<ErrorOr<WeighingSession>> ChangeAsync(string batchId, Func<WeighingSession, ErrorOr<Success>> change, CancellationToken cancellationToken)
    {
        if (await sessions.FindAsync(batchId, cancellationToken) is not { } session)
        {
            return WeighingErrors.SessionNotFound(batchId);
        }

        var changed = change(session);
        return changed.IsError ? changed.Errors : await SaveAsync(session, cancellationToken);
    }

    private async Task<ErrorOr<WeighingSession>> SaveAsync(WeighingSession session, CancellationToken cancellationToken)
    {
        AppendAudit(session);
        var saved = await sessions.SaveAsync(cancellationToken);
        if (saved.IsError)
        {
            return saved.Errors;
        }

        session.ClearEvents();
        return session;
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
            if (ToAudit(e) is { } entry)
            {
                audit.Append(entry);
            }
        }
    }

    /// <summary>
    /// The audit entry for an event, or null for events the record itself carries (lot, container, first tare).
    /// </summary>
    private static AuditEntry? ToAudit(IWeighingEvent e) => e switch
    {
        WeighingSessionStarted s => AuditEntry.Record(AuditEventType.WeighingSessionStarted, s.OperatorBadgeId, s.OccurredAt, s.BatchId),
        WeighingSessionResumed r => AuditEntry.Record(AuditEventType.WeighingSessionResumed, r.OperatorBadgeId, r.OccurredAt, r.BatchId),
        ComponentSelected c => AuditEntry.Record(AuditEventType.ComponentSelected, c.OperatorBadgeId, c.OccurredAt, $"{c.BatchId}#{c.Sequence}"),
        WeighingAttemptAbandoned a => AuditEntry.Record(AuditEventType.AttemptAbandoned, a.OperatorBadgeId, a.OccurredAt, $"{a.BatchId}#{a.Sequence}"),
        ScaleReTared t => AuditEntry.Record(AuditEventType.ReTare, t.OperatorBadgeId, t.OccurredAt, $"{t.BatchId}#{t.Sequence}", $"tare {t.Previous.Tare} → {t.Tare.Tare}"),
        WeighingConfirmed w => AuditEntry.Record(AuditEventType.WeighingConfirmed, w.OperatorBadgeId, w.OccurredAt, $"{w.BatchId}#{w.Record.Sequence}"),
        WeighingAttemptQuit q => AuditEntry.Record(AuditEventType.Quit, q.OperatorBadgeId, q.OccurredAt, $"{q.BatchId}#{q.Sequence}"),
        BatchLeft l => AuditEntry.Record(AuditEventType.BatchLeft, l.OperatorBadgeId, l.OccurredAt, l.BatchId, $"{l.DoneCount} of {l.Total} done"),
        LotCaptured or ContainerCaptured or ScaleTared => null,
        _ => throw new InvalidOperationException($"Unmapped weighing event {e.GetType().Name}."),
    };
}
