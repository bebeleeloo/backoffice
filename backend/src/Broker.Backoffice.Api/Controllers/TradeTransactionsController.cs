using Broker.Backoffice.Api.Filters;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.Transactions.TradeTransactions;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[Route("api/v1/trade-transactions")]
public sealed class TradeTransactionsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.TransactionsRead)]
    public async Task<ActionResult<PagedResult<TradeTransactionListItemDto>>> List(
        [FromQuery] GetTradeTransactionsQuery query, CancellationToken ct)
    {
        return Ok(await mediator.Send(query, ct));
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.TransactionsRead)]
    public async Task<ActionResult<TradeTransactionDto>> Get(Guid id, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetTradeTransactionByIdQuery(id), ct));
    }

    [HttpGet("by-order/{orderId:guid}")]
    [HasPermission(Permissions.TransactionsRead)]
    public async Task<ActionResult<List<TradeTransactionListItemDto>>> GetByOrder(Guid orderId, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetTradeTransactionsByOrderIdQuery(orderId), ct));
    }

    [HttpPost]
    [HasPermission(Permissions.TransactionsCreate)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<ActionResult<TradeTransactionDto>> Create(CreateTradeTransactionCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.TransactionsUpdate)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<ActionResult<TradeTransactionDto>> Update(Guid id, UpdateTradeTransactionCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id mismatch");
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.TransactionsDelete)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteTradeTransactionCommand(id), ct);
        return NoContent();
    }
}
