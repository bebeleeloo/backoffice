using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Orders;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Orders.NonTradeOrders;

public sealed record UpdateNonTradeOrderCommand(
    Guid Id,
    Guid AccountId,
    DateTime OrderDate,
    OrderStatus Status,
    NonTradeOrderType NonTradeType,
    decimal Amount,
    Guid CurrencyId,
    Guid? InstrumentId,
    string? ReferenceNumber,
    string? Description,
    DateTime? ProcessedAt,
    string? Comment,
    string? ExternalId,
    byte[] RowVersion) : IRequest<NonTradeOrderDto>;

public sealed class UpdateNonTradeOrderCommandValidator : AbstractValidator<UpdateNonTradeOrderCommand>
{
    public UpdateNonTradeOrderCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.Amount).NotEqual(0);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Comment).MaximumLength(500);
        RuleFor(x => x.ExternalId).MaximumLength(64);
    }
}

public sealed class UpdateNonTradeOrderCommandHandler(
    IAppDbContext db,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    IAuditContext audit) : IRequestHandler<UpdateNonTradeOrderCommand, NonTradeOrderDto>
{
    public async Task<NonTradeOrderDto> Handle(UpdateNonTradeOrderCommand request, CancellationToken ct)
    {
        var nonTrade = await db.NonTradeOrders
            .Include(n => n.Order!)
            .FirstOrDefaultAsync(n => n.OrderId == request.Id, ct)
            ?? throw new KeyNotFoundException($"Non-trade order {request.Id} not found");

        var order = nonTrade.Order!;
        var before = JsonSerializer.Serialize(new { order.Id, order.OrderNumber, order.Status });
        db.Orders.Entry(order).Property(o => o.RowVersion).OriginalValue = request.RowVersion;

        order.AccountId = request.AccountId;
        order.Status = request.Status;
        order.OrderDate = request.OrderDate;
        order.Comment = request.Comment;
        order.ExternalId = request.ExternalId;
        order.UpdatedAt = clock.UtcNow;
        order.UpdatedBy = currentUser.UserName;

        nonTrade.NonTradeType = request.NonTradeType;
        nonTrade.Amount = request.Amount;
        nonTrade.CurrencyId = request.CurrencyId;
        nonTrade.InstrumentId = request.InstrumentId;
        nonTrade.ReferenceNumber = request.ReferenceNumber;
        nonTrade.Description = request.Description;
        nonTrade.ProcessedAt = request.ProcessedAt;

        await db.SaveChangesAsync(ct);

        var result = await new GetNonTradeOrderByIdQueryHandler(db)
            .Handle(new GetNonTradeOrderByIdQuery(order.Id), ct);

        audit.EntityType = "Order";
        audit.EntityId = order.Id.ToString();
        audit.BeforeJson = before;
        audit.AfterJson = JsonSerializer.Serialize(new { order.Id, order.OrderNumber, order.Status });

        return result;
    }
}
