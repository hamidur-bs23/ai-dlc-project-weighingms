using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WeighingMS.Api.Common;

/// <summary>
/// Validates the antiforgery token (<c>X-XSRF-TOKEN</c> header) on every state-changing request
/// (cookie auth requires CSRF protection). Failures are 400 ProblemDetails <c>Request.AntiforgeryInvalid</c>.
/// </summary>
public sealed class AntiforgeryValidationFilter(IAntiforgery antiforgery) : IAsyncAuthorizationFilter
{
    /// <inheritdoc />
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method))
        {
            return;
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "Request.AntiforgeryInvalid",
                Title = "Missing or invalid antiforgery token",
            };
            problem.Extensions["errorCode"] = "Request.AntiforgeryInvalid";
            problem.Extensions["traceId"] = TraceIds.Of(context.HttpContext);
            context.Result = new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
        }
    }
}
