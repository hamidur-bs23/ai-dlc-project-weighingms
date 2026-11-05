namespace WeighingMS.Domain.Weighing;

/// <summary>Where a component is in the session.</summary>
public enum ComponentState
{
    /// <summary>Not weighed yet.</summary>
    Pending,

    /// <summary>Being weighed now (at most one per session).</summary>
    Active,

    /// <summary>Confirmed; final (bolt 008).</summary>
    Done,
}
