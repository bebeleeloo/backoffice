using Asp.Versioning;
using Broker.Auth.Application.Common;
using Broker.Auth.Application.EntityChanges;
using Broker.Auth.Domain.Identity;
using Broker.Auth.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Auth.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/auth/entity-changes")]
public sealed class EntityChangesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.AuditRead)]
    public async Task<ActionResult<PagedResult<OperationDto>>> GetEntityChanges(
        [FromQuery] string entityType, [FromQuery] Guid entityId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetEntityChangesQuery(entityType, entityId, page, pageSize), ct);
        return Ok(result);
    }
}
