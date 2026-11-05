using System.Diagnostics;
using OpenTelemetry;

namespace WeighingMS.Api.Common;

/// <summary>
/// Drops healthy live-weight polls from tracing after they finish (coding-standards: healthy polls are not traced;
/// failed polls are). A span for <c>GET …/api/scales/{id}/weight</c> that ended with a status below 400 is un-recorded,
/// so exporters skip it; failed polls keep their full trace, including the <c>wms.scale.read</c> error span.
/// Must be added before any exporter.
/// </summary>
public sealed class HealthyWeightPollFilter : BaseProcessor<Activity>
{
    /// <inheritdoc />
    public override void OnEnd(Activity data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var method = data.GetTagItem("http.request.method") as string;
        var path = data.GetTagItem("url.path") as string ?? (data.GetTagItem("url.full") as string is { } full ? new Uri(full).AbsolutePath : null);
        var status = data.GetTagItem("http.response.status_code") is int code ? code : 0;
        if (method == "GET" && ObservabilityExtensions.IsWeightPoll(method, path) && status is > 0 and < 400 && data.Status != ActivityStatusCode.Error)
        {
            data.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}
