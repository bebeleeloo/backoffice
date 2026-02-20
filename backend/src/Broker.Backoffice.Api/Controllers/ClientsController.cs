using Broker.Backoffice.Api.Filters;
using Broker.Backoffice.Application.Clients;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[Route("api/v1/clients")]
public sealed class ClientsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.ClientsRead)]
    public async Task<ActionResult<PagedResult<ClientListItemDto>>> List(
        [FromQuery] GetClientsQuery query, CancellationToken ct)
    {
        return Ok(await mediator.Send(query, ct));
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.ClientsRead)]
    public async Task<ActionResult<ClientDto>> Get(Guid id, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetClientByIdQuery(id), ct));
    }

    [HttpPost]
    [HasPermission(Permissions.ClientsCreate)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<ActionResult<ClientDto>> Create(CreateClientCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.ClientsUpdate)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<ActionResult<ClientDto>> Update(Guid id, UpdateClientCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id mismatch");
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.ClientsDelete)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteClientCommand(id), ct);
        return NoContent();
    }
}
