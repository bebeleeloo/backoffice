using Asp.Versioning;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.EntityChanges;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/entity-changes")]
[HasPermission(Permissions.AuditRead)]
public sealed class EntityChangesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<OperationDto>>> List(
        [FromQuery] string entityType,
        [FromQuery] Guid entityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetEntityChangesQuery(entityType, entityId, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("all")]
    public async Task<ActionResult<PagedResult<GlobalOperationDto>>> All(
        [FromQuery] GetAllEntityChangesQuery query, CancellationToken ct)
    {
        return Ok(await mediator.Send(query, ct));
    }
}
