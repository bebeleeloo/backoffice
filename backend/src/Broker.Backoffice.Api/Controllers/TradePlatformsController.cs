using Asp.Versioning;
using Broker.Backoffice.Application.TradePlatforms;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/trade-platforms")]
public sealed class TradePlatformsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.AccountsRead)]
    public async Task<ActionResult<List<TradePlatformDto>>> List(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetTradePlatformsQuery(), ct));
    }

    [HttpGet("all")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<List<TradePlatformDto>>> ListAll(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetAllTradePlatformsQuery(), ct));
    }

    [HttpPost]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<TradePlatformDto>> Create(CreateTradePlatformCommand command, CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<TradePlatformDto>> Update(Guid id, UpdateTradePlatformCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest();
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteTradePlatformCommand(id), ct);
        return NoContent();
    }
}
