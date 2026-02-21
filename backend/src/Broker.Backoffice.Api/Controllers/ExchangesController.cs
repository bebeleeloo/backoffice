using Broker.Backoffice.Application.Exchanges;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[Route("api/v1/exchanges")]
public sealed class ExchangesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.InstrumentsRead)]
    public async Task<ActionResult<List<ExchangeDto>>> List(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetExchangesQuery(), ct));
    }
}
