using Microsoft.AspNetCore.Mvc;

namespace WeighingMS.Api.Common;

/// <summary>Writes ProblemDetails outside MVC (authentication events, rate limiter, antiforgery filter).</summary>
public static class ProblemResponses
{
    /// <summary>Writes a ProblemDetails body with the given status and error code.</summary>
    public static async Task WriteAsync(HttpContext context, int status, string code, string title)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Response.StatusCode = status;
        var service = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        var problem = new ProblemDetails { Status = status, Type = code, Title = title };
        problem.Extensions["errorCode"] = code;
        await service.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem });
    }
}
