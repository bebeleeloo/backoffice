using Asp.Versioning;
using Broker.Backoffice.Application.Currencies;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/currencies")]
public sealed class CurrenciesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.InstrumentsRead)]
    public async Task<ActionResult<List<CurrencyDto>>> List(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetCurrenciesQuery(), ct));
    }

    [HttpGet("all")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<List<CurrencyDto>>> ListAll(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetAllCurrenciesQuery(), ct));
    }

    [HttpPost]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<CurrencyDto>> Create(CreateCurrencyCommand command, CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<CurrencyDto>> Update(Guid id, UpdateCurrencyCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest();
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteCurrencyCommand(id), ct);
        return NoContent();
    }
}
