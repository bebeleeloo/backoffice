using Broker.Backoffice.Application.TradePlatforms;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[Route("api/v1/trade-platforms")]
public sealed class TradePlatformsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.AccountsRead)]
    public async Task<ActionResult<List<TradePlatformDto>>> List(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetTradePlatformsQuery(), ct));
    }
}
