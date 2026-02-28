using Asp.Versioning;
using Broker.Backoffice.Application.Countries;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/countries")]
public sealed class CountriesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.ClientsRead)]
    public async Task<ActionResult<List<CountryDto>>> List(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetCountriesQuery(), ct));
    }
}
