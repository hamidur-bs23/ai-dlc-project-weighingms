namespace WeighingMS.Application.Batches;

/// <summary>
/// WeighingMS status of a batch in the queue. Completed / print-pending / sync-pending are added by the bolts that own
/// those states (009, 011, 012).
/// </summary>
public enum BatchStatus
{
    /// <summary>Released by OrgaTEX, not opened at the station yet.</summary>
    Queued,

    /// <summary>Opened at the station (its recipe snapshot exists).</summary>
    Weighing,

    /// <summary>Completed, written to OrgaTEX and the ticket printed (bolts 011, 012).</summary>
    Complete,

    /// <summary>Completed; the OrgaTEX write-back is still pending (bolt 011).</summary>
    SyncPending,

    /// <summary>Completed; the OrgaTEX write-back failed and needs a manual retry (bolt 011).</summary>
    SyncFailed,

    /// <summary>Completed and written to OrgaTEX; the ticket hasn't been printed yet (bolt 012).</summary>
    PrintPending,
}
