using Broker.Backoffice.Application.AuditLogs;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[Route("api/v1/audit")]
[HasPermission(Permissions.AuditRead)]
public sealed class AuditController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> List(
        [FromQuery] GetAuditLogsQuery query, CancellationToken ct)
    {
        return Ok(await mediator.Send(query, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuditLogDto>> Get(Guid id, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetAuditLogByIdQuery(id), ct));
    }
}
