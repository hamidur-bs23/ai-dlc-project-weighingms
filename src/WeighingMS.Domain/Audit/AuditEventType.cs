namespace WeighingMS.Domain.Audit;

/// <summary>Events written to the append-only audit trail (NFR Security: Audit).</summary>
public enum AuditEventType
{
    /// <summary>An operator signed in.</summary>
    SignIn,

    /// <summary>An operator signed out.</summary>
    SignOut,

    /// <summary>A sign-in replaced the current session at the station.</summary>
    SessionReplaced,

    /// <summary>A supervisor step-up grant was issued.</summary>
    StepUpGranted,

    /// <summary>A supervisor step-up was refused (wrong PIN, non-supervisor badge).</summary>
    StepUpFailed,

    /// <summary>A supervisor was locked out after too many wrong PINs.</summary>
    StepUpLockedOut,

    /// <summary>An out-of-tolerance weighing was confirmed by supervisor override (bolt 009).</summary>
    Override,

    /// <summary>The current weighing attempt was quit (bolt 008).</summary>
    Quit,

    /// <summary>A batch was left incomplete (bolt 008).</summary>
    BatchLeft,

    /// <summary>The scale was re-tared (bolt 008).</summary>
    ReTare,

    /// <summary>A ticket was reprinted (bolt 012).</summary>
    Reprint,

    /// <summary>An OrgaTEX write-back reached "sync failed" (bolt 011).</summary>
    SyncFailed,
}
