using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeighingMS.Api.Common;
using WeighingMS.Api.Contracts;
using WeighingMS.Application.Abstractions;
using WeighingMS.Application.Access;

namespace WeighingMS.Api.Controllers;

/// <summary>Station identity and plant clock (FR-22: stored UTC, shown in the configured IANA zone).</summary>
[Route("api/station")]
[Authorize(Policy = StationPolicies.RequireOperator)]
public sealed class StationController(IPlantClock clock, StationAccessOptions options) : ApiControllerBase
{
    /// <summary>Station id, time zone, and now in UTC and in plant time.</summary>
    [HttpGet]
    [ProducesResponseType<StationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public StationResponse Get()
    {
        var now = clock.UtcNow;
        return new StationResponse(options.StationId, clock.TimeZoneId, now, clock.ToPlantTime(now));
    }
}
