using Asp.Versioning;
using Broker.Auth.Api.Filters;
using Broker.Auth.Application.Common;
using Broker.Auth.Application.Roles;
using Broker.Auth.Domain.Identity;
using Broker.Auth.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Auth.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/roles")]
public sealed class RolesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.RolesRead)]
    public async Task<ActionResult<PagedResult<RoleDto>>> List(
        [FromQuery] GetRolesQuery query, CancellationToken ct)
    {
        return Ok(await mediator.Send(query, ct));
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.RolesRead)]
    public async Task<ActionResult<RoleDto>> Get(Guid id, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetRoleByIdQuery(id), ct));
    }

    [HttpPost]
    [HasPermission(Permissions.RolesCreate)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<ActionResult<RoleDto>> Create(CreateRoleCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.RolesUpdate)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<ActionResult<RoleDto>> Update(Guid id, UpdateRoleCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id mismatch");
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.RolesDelete)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteRoleCommand(id), ct);
        return NoContent();
    }

    public sealed record SetPermissionsBody(List<Guid> PermissionIds, uint RowVersion);

    [HttpPut("{id:guid}/permissions")]
    [HasPermission(Permissions.RolesUpdate)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<ActionResult<RoleDto>> SetPermissions(
        Guid id, [FromBody] SetPermissionsBody body, CancellationToken ct)
    {
        return Ok(await mediator.Send(new SetRolePermissionsCommand(id, body.PermissionIds, body.RowVersion), ct));
    }
}
