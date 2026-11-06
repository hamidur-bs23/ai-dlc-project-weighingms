namespace WeighingMS.Domain.WriteBack;

/// <summary>Where a batch's consumption write-back to OrgaTEX stands (PD-01).</summary>
public enum WriteBackStatus
{
    /// <summary>Waiting to be posted, or between automatic retries.</summary>
    Pending,

    /// <summary>OrgaTEX confirmed it (200 OK).</summary>
    Sent,

    /// <summary>Automatic retries gave up, or OrgaTEX rejected it; needs a manual Retry.</summary>
    SyncFailed,
}
