using Asp.Versioning;
using Broker.Backoffice.Api.Filters;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.Instruments;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/instruments")]
public sealed class InstrumentsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.InstrumentsRead)]
    public async Task<ActionResult<PagedResult<InstrumentListItemDto>>> List(
        [FromQuery] GetInstrumentsQuery query, CancellationToken ct)
    {
        return Ok(await mediator.Send(query, ct));
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.InstrumentsRead)]
    public async Task<ActionResult<InstrumentDto>> Get(Guid id, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetInstrumentByIdQuery(id), ct));
    }

    [HttpPost]
    [HasPermission(Permissions.InstrumentsCreate)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<ActionResult<InstrumentDto>> Create(CreateInstrumentCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.InstrumentsUpdate)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<ActionResult<InstrumentDto>> Update(Guid id, UpdateInstrumentCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id mismatch");
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.InstrumentsDelete)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteInstrumentCommand(id), ct);
        return NoContent();
    }
}
