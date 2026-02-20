using Broker.Backoffice.Application.Permissions;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[Route("api/v1/permissions")]
public sealed class PermissionsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.PermissionsRead)]
    public async Task<ActionResult<IReadOnlyList<PermissionDto>>> List(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetPermissionsQuery(), ct));
    }
}
