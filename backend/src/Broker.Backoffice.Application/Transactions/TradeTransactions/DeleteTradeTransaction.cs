using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Transactions.TradeTransactions;

public sealed record DeleteTradeTransactionCommand(Guid Id) : IRequest;

public sealed class DeleteTradeTransactionCommandHandler(
    IAppDbContext db,
    IAuditContext audit) : IRequestHandler<DeleteTradeTransactionCommand>
{
    public async Task Handle(DeleteTradeTransactionCommand request, CancellationToken ct)
    {
        var transaction = await db.Transactions
            .Where(t => t.Category == OrderCategory.Trade)
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Trade transaction {request.Id} not found");

        audit.EntityType = "Transaction";
        audit.EntityId = transaction.Id.ToString();
        audit.BeforeJson = JsonSerializer.Serialize(new { transaction.Id, transaction.TransactionNumber, transaction.Status, transaction.Category });

        db.Transactions.Remove(transaction);
        await db.SaveChangesAsync(ct);
    }
}
