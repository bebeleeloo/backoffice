using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Orders;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Orders.NonTradeOrders;

public sealed record CreateNonTradeOrderCommand(
    Guid AccountId,
    DateTime OrderDate,
    NonTradeOrderType NonTradeType,
    decimal Amount,
    Guid CurrencyId,
    Guid? InstrumentId,
    string? ReferenceNumber,
    string? Description,
    string? Comment,
    string? ExternalId) : IRequest<NonTradeOrderDto>;

public sealed class CreateNonTradeOrderCommandValidator : AbstractValidator<CreateNonTradeOrderCommand>
{
    public CreateNonTradeOrderCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.Amount).NotEqual(0);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Comment).MaximumLength(500);
        RuleFor(x => x.ExternalId).MaximumLength(64);
    }
}

public sealed class CreateNonTradeOrderCommandHandler(
    IAppDbContext db,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    IAuditContext audit) : IRequestHandler<CreateNonTradeOrderCommand, NonTradeOrderDto>
{
    public async Task<NonTradeOrderDto> Handle(CreateNonTradeOrderCommand request, CancellationToken ct)
    {
        if (!await db.Accounts.AnyAsync(a => a.Id == request.AccountId, ct))
            throw new KeyNotFoundException($"Account {request.AccountId} not found");
        if (!await db.Currencies.AnyAsync(c => c.Id == request.CurrencyId, ct))
            throw new KeyNotFoundException($"Currency {request.CurrencyId} not found");
        if (request.InstrumentId.HasValue && !await db.Instruments.AnyAsync(i => i.Id == request.InstrumentId.Value, ct))
            throw new KeyNotFoundException($"Instrument {request.InstrumentId} not found");

        var orderId = Guid.NewGuid();
        var orderNumber = $"NTO-{clock.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        var order = new Order
        {
            Id = orderId,
            AccountId = request.AccountId,
            OrderNumber = orderNumber,
            Category = OrderCategory.NonTrade,
            Status = OrderStatus.New,
            OrderDate = request.OrderDate,
            Comment = request.Comment,
            ExternalId = request.ExternalId,
            CreatedAt = clock.UtcNow,
            CreatedBy = currentUser.UserName
        };

        var nonTradeOrder = new NonTradeOrder
        {
            OrderId = orderId,
            NonTradeType = request.NonTradeType,
            Amount = request.Amount,
            CurrencyId = request.CurrencyId,
            InstrumentId = request.InstrumentId,
            ReferenceNumber = request.ReferenceNumber,
            Description = request.Description
        };

        db.Orders.Add(order);
        db.NonTradeOrders.Add(nonTradeOrder);
        await db.SaveChangesAsync(ct);

        var result = await new GetNonTradeOrderByIdQueryHandler(db)
            .Handle(new GetNonTradeOrderByIdQuery(orderId), ct);

        audit.EntityType = "Order";
        audit.EntityId = order.Id.ToString();
        audit.AfterJson = JsonSerializer.Serialize(new { order.Id, order.OrderNumber, order.Status, order.Category });

        return result;
    }
}
