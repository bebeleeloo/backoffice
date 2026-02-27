using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Transactions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Transactions.NonTradeTransactions;

public sealed record GetNonTradeTransactionByIdQuery(Guid Id) : IRequest<NonTradeTransactionDto>;

public sealed class GetNonTradeTransactionByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetNonTradeTransactionByIdQuery, NonTradeTransactionDto>
{
    public async Task<NonTradeTransactionDto> Handle(GetNonTradeTransactionByIdQuery request, CancellationToken ct)
    {
        var nonTrade = await db.NonTradeTransactions
            .Include(n => n.Transaction!).ThenInclude(tx => tx.Order!).ThenInclude(o => o.Account)
            .Include(n => n.Currency!)
            .Include(n => n.Instrument)
            .FirstOrDefaultAsync(n => n.TransactionId == request.Id, ct)
            ?? throw new KeyNotFoundException($"Non-trade transaction {request.Id} not found");

        return ToDto(nonTrade);
    }

    internal static NonTradeTransactionDto ToDto(NonTradeTransaction n) => new(
        n.Transaction!.Id,
        n.Transaction.OrderId,
        n.Transaction.Order?.OrderNumber,
        n.Transaction.Order?.Account?.Number,
        n.Transaction.TransactionNumber,
        n.Transaction.Status,
        n.Transaction.TransactionDate,
        n.Transaction.Comment,
        n.Transaction.ExternalId,
        n.Amount,
        n.CurrencyId,
        n.Currency?.Code ?? "",
        n.InstrumentId,
        n.Instrument?.Symbol,
        n.Instrument?.Name,
        n.ReferenceNumber,
        n.Description,
        n.ProcessedAt,
        n.Transaction.CreatedAt,
        n.Transaction.RowVersion);
}
