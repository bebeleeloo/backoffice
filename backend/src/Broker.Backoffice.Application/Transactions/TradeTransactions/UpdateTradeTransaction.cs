using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Orders;
using Broker.Backoffice.Domain.Transactions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Transactions.TradeTransactions;

public sealed record UpdateTradeTransactionCommand(
    Guid Id,
    Guid? OrderId,
    Guid InstrumentId,
    DateTime TransactionDate,
    TransactionStatus Status,
    TradeSide Side,
    decimal Quantity,
    decimal Price,
    decimal? Commission,
    DateTime? SettlementDate,
    string? Venue,
    string? Comment,
    string? ExternalId,
    byte[] RowVersion) : IRequest<TradeTransactionDto>;

public sealed class UpdateTradeTransactionCommandValidator : AbstractValidator<UpdateTradeTransactionCommand>
{
    public UpdateTradeTransactionCommandValidator()
    {
        RuleFor(x => x.InstrumentId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Commission).GreaterThanOrEqualTo(0).When(x => x.Commission.HasValue);
        RuleFor(x => x.Venue).MaximumLength(100);
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Comment).MaximumLength(500);
        RuleFor(x => x.ExternalId).MaximumLength(64);
    }
}

public sealed class UpdateTradeTransactionCommandHandler(
    IAppDbContext db,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    IAuditContext audit) : IRequestHandler<UpdateTradeTransactionCommand, TradeTransactionDto>
{
    public async Task<TradeTransactionDto> Handle(UpdateTradeTransactionCommand request, CancellationToken ct)
    {
        var trade = await db.TradeTransactions
            .Include(t => t.Transaction!)
            .FirstOrDefaultAsync(t => t.TransactionId == request.Id, ct)
            ?? throw new KeyNotFoundException($"Trade transaction {request.Id} not found");

        var transaction = trade.Transaction!;

        if (request.OrderId.HasValue && request.OrderId != transaction.OrderId
            && !await db.Orders.AnyAsync(o => o.Id == request.OrderId.Value && o.Category == OrderCategory.Trade, ct))
            throw new KeyNotFoundException($"Trade order {request.OrderId} not found");
        if (request.InstrumentId != trade.InstrumentId && !await db.Instruments.AnyAsync(i => i.Id == request.InstrumentId, ct))
            throw new KeyNotFoundException($"Instrument {request.InstrumentId} not found");

        var before = JsonSerializer.Serialize(new { transaction.Id, transaction.TransactionNumber, transaction.Status });
        db.Transactions.Entry(transaction).Property(t => t.RowVersion).OriginalValue = request.RowVersion;

        transaction.OrderId = request.OrderId;
        transaction.Status = request.Status;
        transaction.TransactionDate = request.TransactionDate;
        transaction.Comment = request.Comment;
        transaction.ExternalId = request.ExternalId;
        transaction.UpdatedAt = clock.UtcNow;
        transaction.UpdatedBy = currentUser.UserName;

        trade.InstrumentId = request.InstrumentId;
        trade.Side = request.Side;
        trade.Quantity = request.Quantity;
        trade.Price = request.Price;
        trade.Commission = request.Commission;
        trade.SettlementDate = request.SettlementDate;
        trade.Venue = request.Venue;

        await db.SaveChangesAsync(ct);

        var result = await new GetTradeTransactionByIdQueryHandler(db)
            .Handle(new GetTradeTransactionByIdQuery(transaction.Id), ct);

        audit.EntityType = "Transaction";
        audit.EntityId = transaction.Id.ToString();
        audit.BeforeJson = before;
        audit.AfterJson = JsonSerializer.Serialize(new { transaction.Id, transaction.TransactionNumber, transaction.Status });

        return result;
    }
}
