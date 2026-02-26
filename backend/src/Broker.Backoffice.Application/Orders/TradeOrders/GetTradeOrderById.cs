using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Orders.TradeOrders;

public sealed record GetTradeOrderByIdQuery(Guid Id) : IRequest<TradeOrderDto>;

public sealed class GetTradeOrderByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetTradeOrderByIdQuery, TradeOrderDto>
{
    public async Task<TradeOrderDto> Handle(GetTradeOrderByIdQuery request, CancellationToken ct)
    {
        var trade = await db.TradeOrders
            .Include(t => t.Order!).ThenInclude(o => o.Account)
            .Include(t => t.Instrument)
            .FirstOrDefaultAsync(t => t.OrderId == request.Id, ct)
            ?? throw new KeyNotFoundException($"Trade order {request.Id} not found");

        return ToDto(trade);
    }

    internal static TradeOrderDto ToDto(TradeOrder t) => new(
        t.Order!.Id,
        t.Order.AccountId,
        t.Order.Account?.Number ?? "",
        t.Order.OrderNumber,
        t.Order.Status,
        t.Order.OrderDate,
        t.Order.Comment,
        t.Order.ExternalId,
        t.InstrumentId,
        t.Instrument?.Symbol ?? "",
        t.Instrument?.Name ?? "",
        t.Side,
        t.OrderType,
        t.TimeInForce,
        t.Quantity,
        t.Price,
        t.StopPrice,
        t.ExecutedQuantity,
        t.AveragePrice,
        t.Commission,
        t.ExecutedAt,
        t.ExpirationDate,
        t.Order.CreatedAt,
        t.Order.RowVersion);
}
