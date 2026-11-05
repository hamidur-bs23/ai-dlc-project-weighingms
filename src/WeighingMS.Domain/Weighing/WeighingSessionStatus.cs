namespace WeighingMS.Domain.Weighing;

/// <summary>Session status.</summary>
public enum WeighingSessionStatus
{
    /// <summary>Weighing is under way or paused.</summary>
    InProgress,

    /// <summary>Every component recorded; frozen (A17). Raised <see cref="BatchCompleted"/>.</summary>
    Completed,
}
