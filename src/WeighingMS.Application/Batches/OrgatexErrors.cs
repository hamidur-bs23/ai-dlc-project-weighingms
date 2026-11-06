using ErrorOr;

namespace WeighingMS.Application.Batches;

/// <summary>OrgaTEX integration errors.</summary>
public static class OrgatexErrors
{
    /// <summary>OrgaTEX is offline, timing out or failing (503): the UI offers Retry.</summary>
    public static readonly Error Unavailable = Error.Custom(503, "OrgaTex.Unavailable", "OrgaTEX is not reachable.");

    /// <summary>
    /// OrgaTEX refused a consumption post (a 4xx: invalid payload, unknown batch, key conflict). Not retryable
    /// (WRITEBACK-TRANSIENT): the write-back becomes "sync failed" at once. The OrgaTEX error code is in the metadata.
    /// </summary>
    public static Error ConsumptionRejected(string orgatexCode, int status) =>
        Error.Custom(502, "OrgaTex.ConsumptionRejected", $"OrgaTEX rejected the consumption ({status} {orgatexCode}).", new Dictionary<string, object> { ["orgatexCode"] = orgatexCode, ["status"] = status });

    /// <summary>True when a failure is worth retrying automatically: OrgaTEX unreachable, timing out or failing (5xx).</summary>
    public static bool IsTransient(Error error) => error.Code == Unavailable.Code;
}
