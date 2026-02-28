using Asp.Versioning;
using Broker.Backoffice.Application.Exchanges;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/exchanges")]
public sealed class ExchangesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.InstrumentsRead)]
    public async Task<ActionResult<List<ExchangeDto>>> List(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetExchangesQuery(), ct));
    }

    [HttpGet("all")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<List<ExchangeDto>>> ListAll(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetAllExchangesQuery(), ct));
    }

    [HttpPost]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<ExchangeDto>> Create(CreateExchangeCommand command, CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<ExchangeDto>> Update(Guid id, UpdateExchangeCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest();
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteExchangeCommand(id), ct);
        return NoContent();
    }
}
