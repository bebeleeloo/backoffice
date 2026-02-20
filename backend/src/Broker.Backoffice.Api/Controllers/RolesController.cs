using Broker.Backoffice.Api.Filters;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.Roles;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
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

    [HttpPut("{id:guid}/permissions")]
    [HasPermission(Permissions.RolesUpdate)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<ActionResult<RoleDto>> SetPermissions(
        Guid id, [FromBody] List<Guid> permissionIds, CancellationToken ct)
    {
        return Ok(await mediator.Send(new SetRolePermissionsCommand(id, permissionIds), ct));
    }
}
