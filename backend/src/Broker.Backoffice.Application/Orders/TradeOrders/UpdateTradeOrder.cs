using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Orders;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Orders.TradeOrders;

public sealed record UpdateTradeOrderCommand(
    Guid Id,
    Guid AccountId,
    Guid InstrumentId,
    DateTime OrderDate,
    OrderStatus Status,
    TradeSide Side,
    TradeOrderType OrderType,
    TimeInForce TimeInForce,
    decimal Quantity,
    decimal? Price,
    decimal? StopPrice,
    decimal ExecutedQuantity,
    decimal? AveragePrice,
    decimal? Commission,
    DateTime? ExecutedAt,
    DateTime? ExpirationDate,
    string? Comment,
    string? ExternalId,
    byte[] RowVersion) : IRequest<TradeOrderDto>;

public sealed class UpdateTradeOrderCommandValidator : AbstractValidator<UpdateTradeOrderCommand>
{
    public UpdateTradeOrderCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.InstrumentId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThan(0).When(x => x.Price.HasValue);
        RuleFor(x => x.StopPrice).GreaterThan(0).When(x => x.StopPrice.HasValue);
        RuleFor(x => x.ExecutedQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AveragePrice).GreaterThan(0).When(x => x.AveragePrice.HasValue);
        RuleFor(x => x.Commission).GreaterThanOrEqualTo(0).When(x => x.Commission.HasValue);
        RuleFor(x => x.Price).NotEmpty().WithMessage("Price is required for Limit/StopLimit orders")
            .When(x => x.OrderType is TradeOrderType.Limit or TradeOrderType.StopLimit);
        RuleFor(x => x.StopPrice).NotEmpty().WithMessage("Stop Price is required for Stop/StopLimit orders")
            .When(x => x.OrderType is TradeOrderType.Stop or TradeOrderType.StopLimit);
        RuleFor(x => x.ExpirationDate).NotEmpty().WithMessage("Expiration Date is required for GTD orders")
            .When(x => x.TimeInForce == TimeInForce.GTD);
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Comment).MaximumLength(500);
        RuleFor(x => x.ExternalId).MaximumLength(64);
    }
}

public sealed class UpdateTradeOrderCommandHandler(
    IAppDbContext db,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    IAuditContext audit) : IRequestHandler<UpdateTradeOrderCommand, TradeOrderDto>
{
    public async Task<TradeOrderDto> Handle(UpdateTradeOrderCommand request, CancellationToken ct)
    {
        var trade = await db.TradeOrders
            .Include(t => t.Order!)
            .FirstOrDefaultAsync(t => t.OrderId == request.Id, ct)
            ?? throw new KeyNotFoundException($"Trade order {request.Id} not found");

        var order = trade.Order!;

        if (request.AccountId != order.AccountId && !await db.Accounts.AnyAsync(a => a.Id == request.AccountId, ct))
            throw new KeyNotFoundException($"Account {request.AccountId} not found");
        if (request.InstrumentId != trade.InstrumentId && !await db.Instruments.AnyAsync(i => i.Id == request.InstrumentId, ct))
            throw new KeyNotFoundException($"Instrument {request.InstrumentId} not found");

        var before = JsonSerializer.Serialize(new { order.Id, order.OrderNumber, order.Status });
        db.Orders.Entry(order).Property(o => o.RowVersion).OriginalValue = request.RowVersion;

        order.AccountId = request.AccountId;
        order.Status = request.Status;
        order.OrderDate = request.OrderDate;
        order.Comment = request.Comment;
        order.ExternalId = request.ExternalId;
        order.UpdatedAt = clock.UtcNow;
        order.UpdatedBy = currentUser.UserName;

        trade.InstrumentId = request.InstrumentId;
        trade.Side = request.Side;
        trade.OrderType = request.OrderType;
        trade.TimeInForce = request.TimeInForce;
        trade.Quantity = request.Quantity;
        trade.Price = request.Price;
        trade.StopPrice = request.StopPrice;
        trade.ExecutedQuantity = request.ExecutedQuantity;
        trade.AveragePrice = request.AveragePrice;
        trade.Commission = request.Commission;
        trade.ExecutedAt = request.ExecutedAt;
        trade.ExpirationDate = request.ExpirationDate;

        await db.SaveChangesAsync(ct);

        var result = await new GetTradeOrderByIdQueryHandler(db)
            .Handle(new GetTradeOrderByIdQuery(order.Id), ct);

        audit.EntityType = "Order";
        audit.EntityId = order.Id.ToString();
        audit.BeforeJson = before;
        audit.AfterJson = JsonSerializer.Serialize(new { order.Id, order.OrderNumber, order.Status });

        return result;
    }
}
