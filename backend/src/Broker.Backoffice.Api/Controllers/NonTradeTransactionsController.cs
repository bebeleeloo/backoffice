using Broker.Backoffice.Api.Filters;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.Transactions.NonTradeTransactions;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[Route("api/v1/non-trade-transactions")]
public sealed class NonTradeTransactionsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.TransactionsRead)]
    public async Task<ActionResult<PagedResult<NonTradeTransactionListItemDto>>> List(
        [FromQuery] GetNonTradeTransactionsQuery query, CancellationToken ct)
    {
        return Ok(await mediator.Send(query, ct));
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.TransactionsRead)]
    public async Task<ActionResult<NonTradeTransactionDto>> Get(Guid id, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetNonTradeTransactionByIdQuery(id), ct));
    }

    [HttpGet("by-order/{orderId:guid}")]
    [HasPermission(Permissions.TransactionsRead)]
    public async Task<ActionResult<List<NonTradeTransactionListItemDto>>> GetByOrder(Guid orderId, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetNonTradeTransactionsByOrderIdQuery(orderId), ct));
    }

    [HttpPost]
    [HasPermission(Permissions.TransactionsCreate)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<ActionResult<NonTradeTransactionDto>> Create(CreateNonTradeTransactionCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.TransactionsUpdate)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<ActionResult<NonTradeTransactionDto>> Update(Guid id, UpdateNonTradeTransactionCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id mismatch");
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.TransactionsDelete)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteNonTradeTransactionCommand(id), ct);
        return NoContent();
    }
}
