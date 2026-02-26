using Broker.Backoffice.Api.Filters;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.Orders.NonTradeOrders;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Backoffice.Api.Controllers;

[ApiController]
[Route("api/v1/non-trade-orders")]
public sealed class NonTradeOrdersController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.OrdersRead)]
    public async Task<ActionResult<PagedResult<NonTradeOrderListItemDto>>> List(
        [FromQuery] GetNonTradeOrdersQuery query, CancellationToken ct)
    {
        return Ok(await mediator.Send(query, ct));
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.OrdersRead)]
    public async Task<ActionResult<NonTradeOrderDto>> Get(Guid id, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetNonTradeOrderByIdQuery(id), ct));
    }

    [HttpPost]
    [HasPermission(Permissions.OrdersCreate)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<ActionResult<NonTradeOrderDto>> Create(CreateNonTradeOrderCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.OrdersUpdate)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<ActionResult<NonTradeOrderDto>> Update(Guid id, UpdateNonTradeOrderCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id mismatch");
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.OrdersDelete)]
    [ServiceFilter(typeof(AuditActionFilter))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteNonTradeOrderCommand(id), ct);
        return NoContent();
    }
}
