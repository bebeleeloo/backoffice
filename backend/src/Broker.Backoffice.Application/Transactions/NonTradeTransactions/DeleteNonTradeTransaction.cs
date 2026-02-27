using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Transactions.NonTradeTransactions;

public sealed record DeleteNonTradeTransactionCommand(Guid Id) : IRequest;

public sealed class DeleteNonTradeTransactionCommandHandler(
    IAppDbContext db,
    IAuditContext audit) : IRequestHandler<DeleteNonTradeTransactionCommand>
{
    public async Task Handle(DeleteNonTradeTransactionCommand request, CancellationToken ct)
    {
        var transaction = await db.Transactions
            .Where(t => t.Category == OrderCategory.NonTrade)
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Non-trade transaction {request.Id} not found");

        audit.EntityType = "Transaction";
        audit.EntityId = transaction.Id.ToString();
        audit.BeforeJson = JsonSerializer.Serialize(new { transaction.Id, transaction.TransactionNumber, transaction.Status, transaction.Category });

        db.Transactions.Remove(transaction);
        await db.SaveChangesAsync(ct);
    }
}
