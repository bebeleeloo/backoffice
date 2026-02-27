using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Orders;
using Broker.Backoffice.Domain.Transactions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Transactions.TradeTransactions;

public sealed record CreateTradeTransactionCommand(
    Guid? OrderId,
    Guid InstrumentId,
    DateTime TransactionDate,
    TradeSide Side,
    decimal Quantity,
    decimal Price,
    decimal? Commission,
    DateTime? SettlementDate,
    string? Venue,
    string? Comment,
    string? ExternalId) : IRequest<TradeTransactionDto>;

public sealed class CreateTradeTransactionCommandValidator : AbstractValidator<CreateTradeTransactionCommand>
{
    public CreateTradeTransactionCommandValidator()
    {
        RuleFor(x => x.InstrumentId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Commission).GreaterThanOrEqualTo(0).When(x => x.Commission.HasValue);
        RuleFor(x => x.Venue).MaximumLength(100);
        RuleFor(x => x.Comment).MaximumLength(500);
        RuleFor(x => x.ExternalId).MaximumLength(64);
    }
}

public sealed class CreateTradeTransactionCommandHandler(
    IAppDbContext db,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    IAuditContext audit) : IRequestHandler<CreateTradeTransactionCommand, TradeTransactionDto>
{
    public async Task<TradeTransactionDto> Handle(CreateTradeTransactionCommand request, CancellationToken ct)
    {
        if (request.OrderId.HasValue && !await db.Orders.AnyAsync(o => o.Id == request.OrderId.Value && o.Category == OrderCategory.Trade, ct))
            throw new KeyNotFoundException($"Trade order {request.OrderId} not found");
        if (!await db.Instruments.AnyAsync(i => i.Id == request.InstrumentId, ct))
            throw new KeyNotFoundException($"Instrument {request.InstrumentId} not found");

        var transactionId = Guid.NewGuid();
        var transactionNumber = $"TT-{clock.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        var transaction = new Transaction
        {
            Id = transactionId,
            OrderId = request.OrderId,
            TransactionNumber = transactionNumber,
            Category = OrderCategory.Trade,
            Status = TransactionStatus.Pending,
            TransactionDate = request.TransactionDate,
            Comment = request.Comment,
            ExternalId = request.ExternalId,
            CreatedAt = clock.UtcNow,
            CreatedBy = currentUser.UserName
        };

        var tradeTransaction = new TradeTransaction
        {
            TransactionId = transactionId,
            InstrumentId = request.InstrumentId,
            Side = request.Side,
            Quantity = request.Quantity,
            Price = request.Price,
            Commission = request.Commission,
            SettlementDate = request.SettlementDate,
            Venue = request.Venue
        };

        db.Transactions.Add(transaction);
        db.TradeTransactions.Add(tradeTransaction);
        await db.SaveChangesAsync(ct);

        var result = await new GetTradeTransactionByIdQueryHandler(db)
            .Handle(new GetTradeTransactionByIdQuery(transactionId), ct);

        audit.EntityType = "Transaction";
        audit.EntityId = transaction.Id.ToString();
        audit.AfterJson = JsonSerializer.Serialize(new { transaction.Id, transaction.TransactionNumber, transaction.Status, transaction.Category });

        return result;
    }
}
