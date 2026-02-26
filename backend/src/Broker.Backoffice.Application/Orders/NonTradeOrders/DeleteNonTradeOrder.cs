using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Orders.NonTradeOrders;

public sealed record DeleteNonTradeOrderCommand(Guid Id) : IRequest;

public sealed class DeleteNonTradeOrderCommandHandler(
    IAppDbContext db,
    IAuditContext audit) : IRequestHandler<DeleteNonTradeOrderCommand>
{
    public async Task Handle(DeleteNonTradeOrderCommand request, CancellationToken ct)
    {
        var order = await db.Orders
            .Where(o => o.Category == OrderCategory.NonTrade)
            .FirstOrDefaultAsync(o => o.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Non-trade order {request.Id} not found");

        audit.EntityType = "Order";
        audit.EntityId = order.Id.ToString();
        audit.BeforeJson = JsonSerializer.Serialize(new { order.Id, order.OrderNumber, order.Status, order.Category });

        db.Orders.Remove(order);
        await db.SaveChangesAsync(ct);
    }
}
