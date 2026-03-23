using Asp.Versioning;
using Broker.Auth.Application.AuditLogs;
using Broker.Auth.Application.Common;
using Broker.Auth.Domain.Identity;
using Broker.Auth.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Auth.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/auth/audit")]
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
