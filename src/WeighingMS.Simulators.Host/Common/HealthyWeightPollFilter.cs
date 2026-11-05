using System.Diagnostics;
using OpenTelemetry;

namespace WeighingMS.Simulators.Host.Common;

/// <summary>
/// Drops healthy <c>GET /api/scales/{id}/weight</c> spans after they finish; failed polls (status ≥ 400) stay traced.
/// Mirrors the Api's filter (the simulator does not reference WeighingMS projects).
/// </summary>
public sealed class HealthyWeightPollFilter : BaseProcessor<Activity>
{
    /// <inheritdoc />
    public override void OnEnd(Activity data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var path = data.GetTagItem("url.path") as string;
        var status = data.GetTagItem("http.response.status_code") is int code ? code : 0;
        if (data.GetTagItem("http.request.method") as string == "GET"
            && path?.StartsWith("/api/scales/", StringComparison.Ordinal) == true
            && path.EndsWith("/weight", StringComparison.Ordinal)
            && status is > 0 and < 400)
        {
            data.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}
