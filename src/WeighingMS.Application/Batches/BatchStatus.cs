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
}
