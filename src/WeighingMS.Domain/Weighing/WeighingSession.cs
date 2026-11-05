using ErrorOr;
using WeighingMS.Domain.Batches;
using WeighingMS.Domain.Common;

namespace WeighingMS.Domain.Weighing;

/// <summary>
/// The weighing of one batch (aggregate root). Created from the immutable recipe snapshot with every component
/// pending; survives quitting and restarts. At most one component is active; done components never change.
/// Selection is free among pending components, with the first pending one in sequence suggested (PD-07).
/// </summary>
public sealed class WeighingSession
{
    private readonly List<ComponentWeighing> _components = [];
    private readonly List<IWeighingEvent> _events = [];

    private WeighingSession()
    {
        BatchId = string.Empty;
        StartedBy = string.Empty;
    }

    /// <summary>Identifier (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>The batch (unique: one session per batch).</summary>
    public string BatchId { get; private set; }

    /// <summary>Status.</summary>
    public WeighingSessionStatus Status { get; private set; }

    /// <summary>When the session started (UTC).</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>Badge of the operator who started it (anyone may resume, SESSION-RESUME).</summary>
    public string StartedBy { get; private set; }

    /// <summary>When the batch completed (UTC), if it has.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Badge of the operator whose last record completed the batch.</summary>
    public string? CompletedBy { get; private set; }

    /// <summary>The active component's sequence, if one is active.</summary>
    public int? ActiveSequence { get; private set; }

    /// <summary>The components in recipe sequence.</summary>
    public IReadOnlyList<ComponentWeighing> Components => [.. _components.OrderBy(c => c.Sequence)];

    /// <summary>The active component, if any.</summary>
    public ComponentWeighing? Active => ActiveSequence is { } s ? _components.Single(c => c.Sequence == s) : null;

    /// <summary>The first pending component in sequence (PD-07: suggested, not enforced).</summary>
    public ComponentWeighing? Suggested => Components.FirstOrDefault(c => c.State == ComponentState.Pending);

    /// <summary>How many components are done.</summary>
    public int DoneCount => _components.Count(c => c.State == ComponentState.Done);

    /// <summary>Every component is confirmed; completion follows (bolt 009).</summary>
    public bool ReadyToComplete => _components.Count > 0 && _components.All(c => c.State == ComponentState.Done);

    /// <summary>Events raised since the session was loaded.</summary>
    public IReadOnlyList<IWeighingEvent> Events => _events;

    /// <summary>
    /// Starts a session: for each recipe line, the scale is chosen by <paramref name="policy"/> and the tolerance by
    /// decision TOL (recipe → <paramref name="configuredTolerance"/> for chemical + class → blocked). Both are fixed from
    /// now on (A1). Fails if an assigned scale is not among <paramref name="scales"/>.
    /// </summary>
    public static ErrorOr<WeighingSession> Start(
        RecipeSnapshot recipe,
        ScaleSelectionPolicy policy,
        IReadOnlyDictionary<string, ScaleProfile> scales,
        Func<string, ScaleClass, Quantity?> configuredTolerance,
        string operatorBadgeId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(scales);
        ArgumentNullException.ThrowIfNull(configuredTolerance);
        ArgumentException.ThrowIfNullOrWhiteSpace(operatorBadgeId);

        var session = new WeighingSession
        {
            Id = Guid.CreateVersion7(),
            BatchId = recipe.BatchId,
            Status = WeighingSessionStatus.InProgress,
            StartedAt = now.ToUniversalTime(),
            StartedBy = operatorBadgeId,
        };

        foreach (var line in recipe.Components)
        {
            var selection = ScaleSelector.Select(line.Target, policy);
            if (!scales.TryGetValue(selection.ScaleId, out var scale))
            {
                return WeighingErrors.ScaleNotAvailable(selection.ScaleId);
            }

            var tolerance = ToleranceResolver.Resolve(line.RecipeTolerance, () => configuredTolerance(line.ChemicalId, selection.ScaleClass));
            session._components.Add(ComponentWeighing.Create(line, selection, scale, tolerance));
        }

        session._events.Add(new WeighingSessionStarted(session.BatchId, operatorBadgeId, session._components.Count, session.StartedAt));
        return session;
    }

    /// <summary>Opens an existing session again; nothing is reset (story 001).</summary>
    public void Resume(string operatorBadgeId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operatorBadgeId);
        if (Status == WeighingSessionStatus.Completed)
        {
            return; // reopening a completed batch only reads it
        }

        _events.Add(new WeighingSessionResumed(BatchId, operatorBadgeId, DoneCount, now.ToUniversalTime()));
    }

    /// <summary>
    /// Makes a pending component active (FR-4). Selecting the active one changes nothing; a done one is refused.
    /// Switching away from an active component abandons its attempt with no record (same as Quit).
    /// </summary>
    public ErrorOr<Success> Select(int sequence, string operatorBadgeId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operatorBadgeId);
        if (Status == WeighingSessionStatus.Completed)
        {
            return WeighingErrors.BatchCompleted(BatchId);
        }

        var target = _components.SingleOrDefault(c => c.Sequence == sequence);
        if (target is null)
        {
            return WeighingErrors.ComponentNotFound(sequence);
        }

        if (target.State == ComponentState.Done)
        {
            return WeighingErrors.ComponentDone(sequence);
        }

        if (target.State == ComponentState.Active)
        {
            return Result.Success;
        }

        var at = now.ToUniversalTime();
        if (Active is { } current)
        {
            current.ReturnToPending();
            _events.Add(new WeighingAttemptAbandoned(BatchId, operatorBadgeId, current.Sequence, at));
        }

        target.Activate(operatorBadgeId, at);
        ActiveSequence = sequence;
        _events.Add(new ComponentSelected(BatchId, operatorBadgeId, sequence, target.ChemicalId, target.ScaleId, target.ToleranceSource, at));
        return Result.Success;
    }

    /// <summary>Captures the lot for the active component's attempt (story 004; A6: trimmed, 1–32 characters).</summary>
    public ErrorOr<Success> SetLot(int sequence, string? lot, string operatorBadgeId, DateTimeOffset now)
    {
        var component = RequireActive(sequence);
        if (component.IsError)
        {
            return component.Errors;
        }

        var value = AttemptText.Create(lot, "lot");
        if (value.IsError)
        {
            return value.Errors;
        }

        component.Value.SetLot(value.Value);
        _events.Add(new LotCaptured(BatchId, operatorBadgeId, sequence, value.Value, now.ToUniversalTime()));
        return Result.Success;
    }

    /// <summary>Captures the container for the active component's attempt (story 004).</summary>
    public ErrorOr<Success> SetContainer(int sequence, string? container, string operatorBadgeId, DateTimeOffset now)
    {
        var component = RequireActive(sequence);
        if (component.IsError)
        {
            return component.Errors;
        }

        var value = AttemptText.Create(container, "container");
        if (value.IsError)
        {
            return value.Errors;
        }

        component.Value.SetContainer(value.Value);
        _events.Add(new ContainerCaptured(BatchId, operatorBadgeId, sequence, value.Value, now.ToUniversalTime()));
        return Result.Success;
    }

    /// <summary>
    /// Stores the tare the component's own scale reported (story 005). A second tare in the same attempt is a re-tare:
    /// it replaces the stored tare and creates no record.
    /// </summary>
    public ErrorOr<Success> RecordTare(int sequence, TareTaken tare, string operatorBadgeId)
    {
        ArgumentNullException.ThrowIfNull(tare);
        var component = RequireActive(sequence);
        if (component.IsError)
        {
            return component.Errors;
        }

        if (tare.ScaleId != component.Value.ScaleId)
        {
            throw new InvalidOperationException($"Tare from '{tare.ScaleId}' offered for component {sequence} on '{component.Value.ScaleId}'.");
        }

        var previous = component.Value.Attempt!.Tare;
        component.Value.SetTare(tare);
        _events.Add(previous is null
            ? new ScaleTared(BatchId, operatorBadgeId, sequence, tare, tare.TakenAt.ToUniversalTime())
            : new ScaleReTared(BatchId, operatorBadgeId, sequence, previous, tare, tare.TakenAt.ToUniversalTime()));
        return Result.Success;
    }

    /// <summary>
    /// Confirms the named component against the server's own reading of its scale (CONFIRM-SERVER-READ). Every rule of
    /// <see cref="ConfirmationRules"/> must hold; then exactly one immutable record is created, and the next pending
    /// component after it becomes active (NEXT-COMPONENT). A component already done → <c>Weighing.AlreadyConfirmed</c>.
    /// </summary>
    public ErrorOr<WeighingRecord> Confirm(int sequence, MeasuredReading reading, string operatorBadgeId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(reading);
        ArgumentException.ThrowIfNullOrWhiteSpace(operatorBadgeId);
        if (Status == WeighingSessionStatus.Completed)
        {
            return WeighingErrors.BatchCompleted(BatchId);
        }

        if (_components.SingleOrDefault(c => c.Sequence == sequence) is { State: ComponentState.Done })
        {
            return WeighingErrors.AlreadyConfirmed(sequence);
        }

        var component = RequireActive(sequence);
        if (component.IsError)
        {
            return component.Errors;
        }

        var check = ConfirmationRules.Check(component.Value, reading);
        if (!check.CanConfirm)
        {
            return WeighingErrors.NotConfirmable(check);
        }

        var at = now.ToUniversalTime();
        var record = WeighingRecord.Create(Id, BatchId, component.Value, check.Classification!, component.Value.Attempt!, operatorBadgeId, at);
        component.Value.Complete(record);
        _events.Add(new WeighingConfirmed(BatchId, operatorBadgeId, record, at));

        AdvanceAfter(sequence, operatorBadgeId, at);
        return record;
    }

    /// <summary>
    /// Records an Underweight or Overweight weighing by supervisor override (FR-15, OVR, PD-05 option 1). Everything
    /// else a confirm needs must hold (A11); the record keeps its true class, the surplus and the override. In range →
    /// <c>Weighing.OverrideNotNeeded</c>. The caller has checked the grant and consumes it in the same transaction.
    /// </summary>
    public ErrorOr<WeighingRecord> Override(int sequence, MeasuredReading reading, OverrideAuthority authority, string? reason, string operatorBadgeId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(reading);
        ArgumentNullException.ThrowIfNull(authority);
        ArgumentException.ThrowIfNullOrWhiteSpace(operatorBadgeId);
        if (Status == WeighingSessionStatus.Completed)
        {
            return WeighingErrors.BatchCompleted(BatchId);
        }

        if (_components.SingleOrDefault(c => c.Sequence == sequence) is { State: ComponentState.Done })
        {
            return WeighingErrors.AlreadyConfirmed(sequence);
        }

        var component = RequireActive(sequence);
        if (component.IsError)
        {
            return component.Errors;
        }

        if (authority.SupervisorBadgeId == operatorBadgeId)
        {
            return Access.AccessErrors.FourEyesRequired;
        }

        var check = ConfirmationRules.Check(component.Value, reading);
        if (check.Classification is { IsConfirmable: true })
        {
            return WeighingErrors.OverrideNotNeeded(sequence);
        }

        var gaps = check.Missing.Where(g => g != ConfirmationGap.InTolerance).ToList();
        if (gaps.Count > 0 || check.Classification is null)
        {
            return WeighingErrors.NotConfirmable(new ConfirmationCheck(gaps.Count > 0 ? gaps : [ConfirmationGap.Reading], check.Classification));
        }

        var text = AttemptText.Create(reason, "reason", OverrideReasonMaxLength);
        if (text.IsError)
        {
            return text.Errors;
        }

        var at = now.ToUniversalTime();
        var details = new OverrideDetails(authority.GrantId, authority.SupervisorBadgeId, authority.SupervisorName, text.Value, at);
        var record = WeighingRecord.Create(Id, BatchId, component.Value, check.Classification, component.Value.Attempt!, operatorBadgeId, at, details);
        component.Value.Complete(record);
        _events.Add(new WeighingOverridden(BatchId, operatorBadgeId, record, at));
        AdvanceAfter(sequence, operatorBadgeId, at);
        return record;
    }

    /// <summary>The longest override reason accepted.</summary>
    public const int OverrideReasonMaxLength = 500;

    /// <summary>
    /// The explicit completion (AUTO-COMPLETE): completed already → nothing to do; anything pending → <c>Batch.Incomplete</c>.
    /// </summary>
    public ErrorOr<Success> Complete(string operatorBadgeId, DateTimeOffset now)
    {
        if (Status == WeighingSessionStatus.Completed)
        {
            return Result.Success;
        }

        if (!ReadyToComplete)
        {
            return WeighingErrors.Incomplete(BatchId, DoneCount, _components.Count);
        }

        MarkCompleted(operatorBadgeId, now.ToUniversalTime());
        return Result.Success;
    }

    /// <summary>Quit (F4): discards the active attempt; no record; the chemical returns to pending (story 012).</summary>
    public ErrorOr<Success> Quit(int sequence, string operatorBadgeId, DateTimeOffset now)
    {
        var component = RequireActive(sequence);
        if (component.IsError)
        {
            return component.Errors;
        }

        component.Value.ReturnToPending();
        ActiveSequence = null;
        _events.Add(new WeighingAttemptQuit(BatchId, operatorBadgeId, sequence, now.ToUniversalTime()));
        return Result.Success;
    }

    /// <summary>
    /// Leaves the batch (LEAVE-DISCARDS): any active attempt is discarded; confirmed records stay; the session stays in
    /// progress. Recorded as <see cref="BatchLeft"/> while components are still pending.
    /// </summary>
    public void Leave(string operatorBadgeId, DateTimeOffset now)
    {
        if (Status == WeighingSessionStatus.Completed)
        {
            return;
        }

        Active?.ReturnToPending();
        ActiveSequence = null;
        if (!ReadyToComplete)
        {
            _events.Add(new BatchLeft(BatchId, operatorBadgeId, DoneCount, _components.Count, now.ToUniversalTime()));
        }
    }

    /// <summary>Forgets the raised events once they are written.</summary>
    public void ClearEvents() => _events.Clear();

    private void AdvanceAfter(int sequence, string operatorBadgeId, DateTimeOffset at)
    {
        ActiveSequence = NextComponentRule.After(_components, sequence);
        if (Active is { } next)
        {
            next.Activate(operatorBadgeId, at);
        }
        else if (ReadyToComplete)
        {
            MarkCompleted(operatorBadgeId, at); // AUTO-COMPLETE: in the same transaction as the last record
        }
    }

    private void MarkCompleted(string operatorBadgeId, DateTimeOffset at)
    {
        Status = WeighingSessionStatus.Completed;
        CompletedAt = at;
        CompletedBy = operatorBadgeId;
        ActiveSequence = null;
        _events.Add(new BatchCompleted(BatchId, operatorBadgeId, [.. Components.Select(c => c.Record!)], at));
    }

    private ErrorOr<ComponentWeighing> RequireActive(int sequence)
    {
        if (Status == WeighingSessionStatus.Completed)
        {
            return WeighingErrors.BatchCompleted(BatchId);
        }

        var component = _components.SingleOrDefault(c => c.Sequence == sequence);
        if (component is null)
        {
            return WeighingErrors.ComponentNotFound(sequence);
        }

        if (component.State == ComponentState.Done)
        {
            return WeighingErrors.AlreadyConfirmed(sequence);
        }

        return component.State == ComponentState.Active ? component : WeighingErrors.ComponentNotActive(sequence);
    }
}
