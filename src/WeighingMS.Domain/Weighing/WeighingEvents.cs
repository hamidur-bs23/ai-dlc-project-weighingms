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

/// <summary>A lot was captured for the active attempt.</summary>
public sealed record LotCaptured(string BatchId, string OperatorBadgeId, int Sequence, string Lot, DateTimeOffset OccurredAt) : IWeighingEvent;

/// <summary>A container was captured for the active attempt.</summary>
public sealed record ContainerCaptured(string BatchId, string OperatorBadgeId, int Sequence, string Container, DateTimeOffset OccurredAt) : IWeighingEvent;

/// <summary>The first tare of the attempt.</summary>
public sealed record ScaleTared(string BatchId, string OperatorBadgeId, int Sequence, TareTaken Tare, DateTimeOffset OccurredAt) : IWeighingEvent;

/// <summary>A re-tare (F2) replacing the attempt's tare; no record is created.</summary>
public sealed record ScaleReTared(string BatchId, string OperatorBadgeId, int Sequence, TareTaken Previous, TareTaken Tare, DateTimeOffset OccurredAt) : IWeighingEvent;

/// <summary>A weighing was confirmed and recorded.</summary>
public sealed record WeighingConfirmed(string BatchId, string OperatorBadgeId, WeighingRecord Record, DateTimeOffset OccurredAt) : IWeighingEvent;

/// <summary>The attempt was quit (F4): discarded, no record.</summary>
public sealed record WeighingAttemptQuit(string BatchId, string OperatorBadgeId, int Sequence, DateTimeOffset OccurredAt) : IWeighingEvent;

/// <summary>The operator left the batch with components still pending.</summary>
public sealed record BatchLeft(string BatchId, string OperatorBadgeId, int DoneCount, int Total, DateTimeOffset OccurredAt) : IWeighingEvent;
