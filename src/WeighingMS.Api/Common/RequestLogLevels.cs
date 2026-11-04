using Serilog.Events;

namespace WeighingMS.Api.Common;

/// <summary>
/// Request log levels (coding-standards → Logging): an unavailable integration (503) is a Warning, other server
/// errors and unhandled exceptions are Errors, everything else is Information.
/// </summary>
public static class RequestLogLevels
{
    /// <summary>The level for one completed request.</summary>
    public static LogEventLevel For(HttpContext context, double elapsedMs, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(context);
        var status = context.Response.StatusCode;
        if (exception is not null || (status >= 500 && status != StatusCodes.Status503ServiceUnavailable))
        {
            return LogEventLevel.Error;
        }

        return status == StatusCodes.Status503ServiceUnavailable ? LogEventLevel.Warning : LogEventLevel.Information;
    }
}
