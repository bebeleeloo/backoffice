using Broker.Backoffice.Application.Clearers;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[Route("api/v1/clearers")]
public sealed class ClearersController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.AccountsRead)]
    public async Task<ActionResult<List<ClearerDto>>> List(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetClearersQuery(), ct));
    }
}
