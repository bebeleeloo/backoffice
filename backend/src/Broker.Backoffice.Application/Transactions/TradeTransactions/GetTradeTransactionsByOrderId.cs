using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Transactions.TradeTransactions;

public sealed record GetTradeTransactionsByOrderIdQuery(Guid OrderId) : IRequest<List<TradeTransactionListItemDto>>;

public sealed class GetTradeTransactionsByOrderIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetTradeTransactionsByOrderIdQuery, List<TradeTransactionListItemDto>>
{
    public async Task<List<TradeTransactionListItemDto>> Handle(GetTradeTransactionsByOrderIdQuery request, CancellationToken ct)
    {
        if (!await db.Orders.AnyAsync(o => o.Id == request.OrderId && o.Category == OrderCategory.Trade, ct))
            throw new KeyNotFoundException($"Trade order {request.OrderId} not found");

        return await db.TradeTransactions
            .Where(t => t.Transaction!.OrderId == request.OrderId)
            .OrderByDescending(t => t.Transaction!.TransactionDate)
            .Select(t => new TradeTransactionListItemDto(
                t.TransactionId,
                t.Transaction!.Order!.OrderNumber,
                t.Transaction.Order.Account!.Number,
                t.Transaction.TransactionNumber,
                t.Transaction.Status,
                t.Transaction.TransactionDate,
                t.Instrument!.Symbol,
                t.Instrument.Name,
                t.Side,
                t.Quantity,
                t.Price,
                t.Commission,
                t.SettlementDate,
                t.Venue,
                t.Transaction.ExternalId,
                t.Transaction.CreatedAt,
                t.Transaction.RowVersion))
            .ToListAsync(ct);
    }
}
