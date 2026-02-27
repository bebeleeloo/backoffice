using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Orders;
using Broker.Backoffice.Domain.Transactions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Transactions.NonTradeTransactions;

public sealed record CreateNonTradeTransactionCommand(
    Guid? OrderId,
    DateTime TransactionDate,
    decimal Amount,
    Guid CurrencyId,
    Guid? InstrumentId,
    string? ReferenceNumber,
    string? Description,
    string? Comment,
    string? ExternalId) : IRequest<NonTradeTransactionDto>;

public sealed class CreateNonTradeTransactionCommandValidator : AbstractValidator<CreateNonTradeTransactionCommand>
{
    public CreateNonTradeTransactionCommandValidator()
    {
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.Amount).NotEqual(0);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Comment).MaximumLength(500);
        RuleFor(x => x.ExternalId).MaximumLength(64);
    }
}

public sealed class CreateNonTradeTransactionCommandHandler(
    IAppDbContext db,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    IAuditContext audit) : IRequestHandler<CreateNonTradeTransactionCommand, NonTradeTransactionDto>
{
    public async Task<NonTradeTransactionDto> Handle(CreateNonTradeTransactionCommand request, CancellationToken ct)
    {
        if (request.OrderId.HasValue && !await db.Orders.AnyAsync(o => o.Id == request.OrderId.Value && o.Category == OrderCategory.NonTrade, ct))
            throw new KeyNotFoundException($"Non-trade order {request.OrderId} not found");
        if (!await db.Currencies.AnyAsync(c => c.Id == request.CurrencyId, ct))
            throw new KeyNotFoundException($"Currency {request.CurrencyId} not found");
        if (request.InstrumentId.HasValue && !await db.Instruments.AnyAsync(i => i.Id == request.InstrumentId.Value, ct))
            throw new KeyNotFoundException($"Instrument {request.InstrumentId} not found");

        var transactionId = Guid.NewGuid();
        var transactionNumber = $"NTT-{clock.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        var transaction = new Transaction
        {
            Id = transactionId,
            OrderId = request.OrderId,
            TransactionNumber = transactionNumber,
            Category = OrderCategory.NonTrade,
            Status = TransactionStatus.Pending,
            TransactionDate = request.TransactionDate,
            Comment = request.Comment,
            ExternalId = request.ExternalId,
            CreatedAt = clock.UtcNow,
            CreatedBy = currentUser.UserName
        };

        var nonTradeTransaction = new NonTradeTransaction
        {
            TransactionId = transactionId,
            Amount = request.Amount,
            CurrencyId = request.CurrencyId,
            InstrumentId = request.InstrumentId,
            ReferenceNumber = request.ReferenceNumber,
            Description = request.Description
        };

        db.Transactions.Add(transaction);
        db.NonTradeTransactions.Add(nonTradeTransaction);
        await db.SaveChangesAsync(ct);

        var result = await new GetNonTradeTransactionByIdQueryHandler(db)
            .Handle(new GetNonTradeTransactionByIdQuery(transactionId), ct);

        audit.EntityType = "Transaction";
        audit.EntityId = transaction.Id.ToString();
        audit.AfterJson = JsonSerializer.Serialize(new { transaction.Id, transaction.TransactionNumber, transaction.Status, transaction.Category });

        return result;
    }
}
