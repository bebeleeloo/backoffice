using Asp.Versioning;
using Broker.Auth.Application.Permissions;
using Broker.Auth.Domain.Identity;
using Broker.Auth.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Auth.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
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
