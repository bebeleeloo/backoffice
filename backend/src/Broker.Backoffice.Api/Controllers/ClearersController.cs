using Broker.Backoffice.Application.Clearers;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[Route("api/v1/clearers")]
public sealed class ClearersController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.AccountsRead)]
    public async Task<ActionResult<List<ClearerDto>>> List(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetClearersQuery(), ct));
    }

    [HttpGet("all")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<List<ClearerDto>>> ListAll(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetAllClearersQuery(), ct));
    }

    [HttpPost]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<ClearerDto>> Create(CreateClearerCommand command, CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<ClearerDto>> Update(Guid id, UpdateClearerCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest();
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteClearerCommand(id), ct);
        return NoContent();
    }
}
