using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeighingMS.Api.Common;

namespace WeighingMS.Api.Controllers;

/// <summary>Issues the <c>XSRF-TOKEN</c> cookie that the SPA echoes back in the <c>X-XSRF-TOKEN</c> header.</summary>
[Route("api/antiforgery")]
[AllowAnonymous]
public sealed class AntiforgeryController(IAntiforgery antiforgery) : ApiControllerBase
{
    /// <summary>Issues a fresh antiforgery token pair for the current identity.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Issue()
    {
        AntiforgeryCookies.Issue(HttpContext, antiforgery);
        return NoContent();
    }
}
