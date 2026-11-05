using ErrorOr;

namespace WeighingMS.Application.Batches;

/// <summary>OrgaTEX integration errors.</summary>
public static class OrgatexErrors
{
    /// <summary>OrgaTEX is offline, timing out or failing (503): the UI offers Retry.</summary>
    public static readonly Error Unavailable = Error.Custom(503, "OrgaTex.Unavailable", "OrgaTEX is not reachable.");
}
