namespace WeighingMS.Domain.Weighing;

/// <summary>Something that happened to a weighing session; written to the audit trail.</summary>
public interface IWeighingEvent
{
    /// <summary>The batch.</summary>
    string BatchId { get; }

    /// <summary>Badge of the operator who caused it.</summary>
    string OperatorBadgeId { get; }

    /// <summary>When (UTC).</summary>
    DateTimeOffset OccurredAt { get; }
}

/// <summary>A batch was opened for weighing for the first time.</summary>
public sealed record WeighingSessionStarted(string BatchId, string OperatorBadgeId, int ComponentCount, DateTimeOffset OccurredAt) : IWeighingEvent;

/// <summary>A batch with an existing session was opened again.</summary>
public sealed record WeighingSessionResumed(string BatchId, string OperatorBadgeId, int DoneCount, DateTimeOffset OccurredAt) : IWeighingEvent;

/// <summary>A pending component became the active one.</summary>
public sealed record ComponentSelected(string BatchId, string OperatorBadgeId, int Sequence, string ChemicalId, string ScaleId, ToleranceSource? ToleranceSource, DateTimeOffset OccurredAt) : IWeighingEvent;

/// <summary>The active component was left before confirming (switching component, FR-4); nothing was recorded.</summary>
public sealed record WeighingAttemptAbandoned(string BatchId, string OperatorBadgeId, int Sequence, DateTimeOffset OccurredAt) : IWeighingEvent;
