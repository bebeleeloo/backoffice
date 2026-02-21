using Broker.Backoffice.Application.Currencies;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[Route("api/v1/currencies")]
public sealed class CurrenciesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.InstrumentsRead)]
    public async Task<ActionResult<List<CurrencyDto>>> List(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetCurrenciesQuery(), ct));
    }
}
