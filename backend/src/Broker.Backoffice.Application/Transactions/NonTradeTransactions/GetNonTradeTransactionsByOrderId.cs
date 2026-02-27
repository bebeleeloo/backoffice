using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Transactions.NonTradeTransactions;

public sealed record GetNonTradeTransactionsByOrderIdQuery(Guid OrderId) : IRequest<List<NonTradeTransactionListItemDto>>;

public sealed class GetNonTradeTransactionsByOrderIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetNonTradeTransactionsByOrderIdQuery, List<NonTradeTransactionListItemDto>>
{
    public async Task<List<NonTradeTransactionListItemDto>> Handle(GetNonTradeTransactionsByOrderIdQuery request, CancellationToken ct)
    {
        if (!await db.Orders.AnyAsync(o => o.Id == request.OrderId && o.Category == OrderCategory.NonTrade, ct))
            throw new KeyNotFoundException($"Non-trade order {request.OrderId} not found");

        return await db.NonTradeTransactions
            .Where(n => n.Transaction!.OrderId == request.OrderId)
            .OrderByDescending(n => n.Transaction!.TransactionDate)
            .Select(n => new NonTradeTransactionListItemDto(
                n.TransactionId,
                n.Transaction!.Order!.OrderNumber,
                n.Transaction.Order.Account!.Number,
                n.Transaction.TransactionNumber,
                n.Transaction.Status,
                n.Transaction.TransactionDate,
                n.Amount,
                n.Currency!.Code,
                n.Instrument != null ? n.Instrument.Symbol : null,
                n.Instrument != null ? n.Instrument.Name : null,
                n.ReferenceNumber,
                n.ProcessedAt,
                n.Transaction.ExternalId,
                n.Transaction.CreatedAt,
                n.Transaction.RowVersion))
            .ToListAsync(ct);
    }
}
