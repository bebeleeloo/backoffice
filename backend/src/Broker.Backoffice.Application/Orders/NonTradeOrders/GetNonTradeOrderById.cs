using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Orders.NonTradeOrders;

public sealed record GetNonTradeOrderByIdQuery(Guid Id) : IRequest<NonTradeOrderDto>;

public sealed class GetNonTradeOrderByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetNonTradeOrderByIdQuery, NonTradeOrderDto>
{
    public async Task<NonTradeOrderDto> Handle(GetNonTradeOrderByIdQuery request, CancellationToken ct)
    {
        var nonTrade = await db.NonTradeOrders
            .Include(n => n.Order!).ThenInclude(o => o.Account)
            .Include(n => n.Currency!)
            .Include(n => n.Instrument)
            .FirstOrDefaultAsync(n => n.OrderId == request.Id, ct)
            ?? throw new KeyNotFoundException($"Non-trade order {request.Id} not found");

        return ToDto(nonTrade);
    }

    internal static NonTradeOrderDto ToDto(NonTradeOrder n) => new(
        n.Order!.Id,
        n.Order.AccountId,
        n.Order.Account?.Number ?? "",
        n.Order.OrderNumber,
        n.Order.Status,
        n.Order.OrderDate,
        n.Order.Comment,
        n.Order.ExternalId,
        n.NonTradeType,
        n.Amount,
        n.CurrencyId,
        n.Currency?.Code ?? "",
        n.InstrumentId,
        n.Instrument?.Symbol,
        n.Instrument?.Name,
        n.ReferenceNumber,
        n.Description,
        n.ProcessedAt,
        n.Order.CreatedAt,
        n.Order.RowVersion);
}
