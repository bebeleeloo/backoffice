using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Orders;
using Broker.Backoffice.Domain.Transactions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Transactions.NonTradeTransactions;

public sealed record UpdateNonTradeTransactionCommand(
    Guid Id,
    Guid? OrderId,
    DateTime TransactionDate,
    TransactionStatus Status,
    decimal Amount,
    Guid CurrencyId,
    Guid? InstrumentId,
    string? ReferenceNumber,
    string? Description,
    DateTime? ProcessedAt,
    string? Comment,
    string? ExternalId,
    byte[] RowVersion) : IRequest<NonTradeTransactionDto>;

public sealed class UpdateNonTradeTransactionCommandValidator : AbstractValidator<UpdateNonTradeTransactionCommand>
{
    public UpdateNonTradeTransactionCommandValidator()
    {
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.Amount).NotEqual(0);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Comment).MaximumLength(500);
        RuleFor(x => x.ExternalId).MaximumLength(64);
    }
}

public sealed class UpdateNonTradeTransactionCommandHandler(
    IAppDbContext db,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    IAuditContext audit) : IRequestHandler<UpdateNonTradeTransactionCommand, NonTradeTransactionDto>
{
    public async Task<NonTradeTransactionDto> Handle(UpdateNonTradeTransactionCommand request, CancellationToken ct)
    {
        var nonTrade = await db.NonTradeTransactions
            .Include(n => n.Transaction!)
            .FirstOrDefaultAsync(n => n.TransactionId == request.Id, ct)
            ?? throw new KeyNotFoundException($"Non-trade transaction {request.Id} not found");

        var transaction = nonTrade.Transaction!;

        if (request.OrderId.HasValue && request.OrderId != transaction.OrderId
            && !await db.Orders.AnyAsync(o => o.Id == request.OrderId.Value && o.Category == OrderCategory.NonTrade, ct))
            throw new KeyNotFoundException($"Non-trade order {request.OrderId} not found");
        if (request.CurrencyId != nonTrade.CurrencyId && !await db.Currencies.AnyAsync(c => c.Id == request.CurrencyId, ct))
            throw new KeyNotFoundException($"Currency {request.CurrencyId} not found");
        if (request.InstrumentId.HasValue && request.InstrumentId != nonTrade.InstrumentId
            && !await db.Instruments.AnyAsync(i => i.Id == request.InstrumentId.Value, ct))
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

        nonTrade.Amount = request.Amount;
        nonTrade.CurrencyId = request.CurrencyId;
        nonTrade.InstrumentId = request.InstrumentId;
        nonTrade.ReferenceNumber = request.ReferenceNumber;
        nonTrade.Description = request.Description;
        nonTrade.ProcessedAt = request.ProcessedAt;

        await db.SaveChangesAsync(ct);

        var result = await new GetNonTradeTransactionByIdQueryHandler(db)
            .Handle(new GetNonTradeTransactionByIdQuery(transaction.Id), ct);

        audit.EntityType = "Transaction";
        audit.EntityId = transaction.Id.ToString();
        audit.BeforeJson = before;
        audit.AfterJson = JsonSerializer.Serialize(new { transaction.Id, transaction.TransactionNumber, transaction.Status });

        return result;
    }
}
