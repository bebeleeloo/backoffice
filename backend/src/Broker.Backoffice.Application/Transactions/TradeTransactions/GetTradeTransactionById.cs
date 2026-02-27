using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Transactions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Transactions.TradeTransactions;

public sealed record GetTradeTransactionByIdQuery(Guid Id) : IRequest<TradeTransactionDto>;

public sealed class GetTradeTransactionByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetTradeTransactionByIdQuery, TradeTransactionDto>
{
    public async Task<TradeTransactionDto> Handle(GetTradeTransactionByIdQuery request, CancellationToken ct)
    {
        var trade = await db.TradeTransactions
            .Include(t => t.Transaction!).ThenInclude(tx => tx.Order!).ThenInclude(o => o.Account)
            .Include(t => t.Instrument)
            .FirstOrDefaultAsync(t => t.TransactionId == request.Id, ct)
            ?? throw new KeyNotFoundException($"Trade transaction {request.Id} not found");

        return ToDto(trade);
    }

    internal static TradeTransactionDto ToDto(TradeTransaction t) => new(
        t.Transaction!.Id,
        t.Transaction.OrderId,
        t.Transaction.Order?.OrderNumber,
        t.Transaction.Order?.Account?.Number,
        t.Transaction.TransactionNumber,
        t.Transaction.Status,
        t.Transaction.TransactionDate,
        t.Transaction.Comment,
        t.Transaction.ExternalId,
        t.InstrumentId,
        t.Instrument?.Symbol ?? "",
        t.Instrument?.Name ?? "",
        t.Side,
        t.Quantity,
        t.Price,
        t.Commission,
        t.SettlementDate,
        t.Venue,
        t.Transaction.CreatedAt,
        t.Transaction.RowVersion);
}
