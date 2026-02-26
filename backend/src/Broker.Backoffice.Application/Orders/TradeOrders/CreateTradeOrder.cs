using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Orders;
using FluentValidation;
using MediatR;

namespace Broker.Backoffice.Application.Orders.TradeOrders;

public sealed record CreateTradeOrderCommand(
    Guid AccountId,
    Guid InstrumentId,
    DateTime OrderDate,
    TradeSide Side,
    TradeOrderType OrderType,
    TimeInForce TimeInForce,
    decimal Quantity,
    decimal? Price,
    decimal? StopPrice,
    decimal? Commission,
    DateTime? ExpirationDate,
    string? Comment,
    string? ExternalId) : IRequest<TradeOrderDto>;

public sealed class CreateTradeOrderCommandValidator : AbstractValidator<CreateTradeOrderCommand>
{
    public CreateTradeOrderCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.InstrumentId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThan(0).When(x => x.Price.HasValue);
        RuleFor(x => x.StopPrice).GreaterThan(0).When(x => x.StopPrice.HasValue);
        RuleFor(x => x.Commission).GreaterThanOrEqualTo(0).When(x => x.Commission.HasValue);
        RuleFor(x => x.Comment).MaximumLength(500);
        RuleFor(x => x.ExternalId).MaximumLength(64);
    }
}

public sealed class CreateTradeOrderCommandHandler(
    IAppDbContext db,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    IAuditContext audit) : IRequestHandler<CreateTradeOrderCommand, TradeOrderDto>
{
    public async Task<TradeOrderDto> Handle(CreateTradeOrderCommand request, CancellationToken ct)
    {
        var orderId = Guid.NewGuid();
        var orderNumber = $"TO-{clock.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        var order = new Order
        {
            Id = orderId,
            AccountId = request.AccountId,
            OrderNumber = orderNumber,
            Category = OrderCategory.Trade,
            Status = OrderStatus.New,
            OrderDate = request.OrderDate,
            Comment = request.Comment,
            ExternalId = request.ExternalId,
            CreatedAt = clock.UtcNow,
            CreatedBy = currentUser.UserName
        };

        var tradeOrder = new TradeOrder
        {
            OrderId = orderId,
            InstrumentId = request.InstrumentId,
            Side = request.Side,
            OrderType = request.OrderType,
            TimeInForce = request.TimeInForce,
            Quantity = request.Quantity,
            Price = request.Price,
            StopPrice = request.StopPrice,
            ExecutedQuantity = 0,
            Commission = request.Commission,
            ExpirationDate = request.ExpirationDate
        };

        db.Orders.Add(order);
        db.TradeOrders.Add(tradeOrder);
        await db.SaveChangesAsync(ct);

        var result = await new GetTradeOrderByIdQueryHandler(db)
            .Handle(new GetTradeOrderByIdQuery(orderId), ct);

        audit.EntityType = "Order";
        audit.EntityId = order.Id.ToString();
        audit.AfterJson = JsonSerializer.Serialize(new { order.Id, order.OrderNumber, order.Status, order.Category });

        return result;
    }
}
