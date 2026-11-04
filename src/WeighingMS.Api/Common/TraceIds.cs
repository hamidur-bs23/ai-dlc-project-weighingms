using System.Diagnostics;

namespace WeighingMS.Api.Common;

/// <summary>The trace id put on every ProblemDetails: the W3C trace context when present, else the request id.</summary>
public static class TraceIds
{
    /// <summary>The current trace id.</summary>
    public static string Of(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Activity.Current?.Id ?? context.TraceIdentifier;
    }
}
