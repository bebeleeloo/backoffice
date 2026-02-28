using Asp.Versioning;
using Broker.Backoffice.Api.Filters;
using Broker.Backoffice.Application.Accounts;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/accounts")]
public sealed class AccountsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.AccountsRead)]
    public async Task<ActionResult<PagedResult<AccountListItemDto>>> List(
        [FromQuery] GetAccountsQuery query, CancellationToken ct)
    {
        return Ok(await mediator.Send(query, ct));
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.AccountsRead)]
    public async Task<ActionResult<AccountDto>> Get(Guid id, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetAccountByIdQuery(id), ct));
    }

    [HttpPost]
    [HasPermission(Permissions.AccountsCreate)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<ActionResult<AccountDto>> Create(CreateAccountCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.AccountsUpdate)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<ActionResult<AccountDto>> Update(Guid id, UpdateAccountCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id mismatch");
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.AccountsDelete)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteAccountCommand(id), ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/holders")]
    [HasPermission(Permissions.AccountsUpdate)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<ActionResult<AccountDto>> SetHolders(
        Guid id, List<AccountHolderInput> holders, CancellationToken ct)
    {
        return Ok(await mediator.Send(new SetAccountHoldersCommand(id, holders), ct));
    }
}
