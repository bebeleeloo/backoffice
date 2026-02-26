using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Orders.TradeOrders;

public sealed record DeleteTradeOrderCommand(Guid Id) : IRequest;

public sealed class DeleteTradeOrderCommandHandler(
    IAppDbContext db,
    IAuditContext audit) : IRequestHandler<DeleteTradeOrderCommand>
{
    public async Task Handle(DeleteTradeOrderCommand request, CancellationToken ct)
    {
        var order = await db.Orders
            .Where(o => o.Category == OrderCategory.Trade)
            .FirstOrDefaultAsync(o => o.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Trade order {request.Id} not found");

        audit.EntityType = "Order";
        audit.EntityId = order.Id.ToString();
        audit.BeforeJson = JsonSerializer.Serialize(new { order.Id, order.OrderNumber, order.Status, order.Category });

        db.Orders.Remove(order);
        await db.SaveChangesAsync(ct);
    }
}
