using Asp.Versioning;
using Broker.Backoffice.Application.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/dashboard")]
[Authorize]
public sealed class DashboardController(ISender mediator) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> Stats(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetDashboardStatsQuery(), ct));
    }
}
