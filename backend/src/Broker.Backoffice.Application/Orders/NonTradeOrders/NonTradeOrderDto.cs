using Broker.Backoffice.Domain.Orders;

namespace Broker.Backoffice.Application.Orders.NonTradeOrders;

public sealed record NonTradeOrderDto(
    Guid Id,
    Guid AccountId,
    string AccountNumber,
    string OrderNumber,
    OrderStatus Status,
    DateTime OrderDate,
    string? Comment,
    string? ExternalId,
    NonTradeOrderType NonTradeType,
    decimal Amount,
    Guid CurrencyId,
    string CurrencyCode,
    Guid? InstrumentId,
    string? InstrumentSymbol,
    string? InstrumentName,
    string? ReferenceNumber,
    string? Description,
    DateTime? ProcessedAt,
    DateTime CreatedAt,
    byte[] RowVersion);

public sealed record NonTradeOrderListItemDto(
    Guid Id,
    string AccountNumber,
    string OrderNumber,
    OrderStatus Status,
    DateTime OrderDate,
    NonTradeOrderType NonTradeType,
    decimal Amount,
    string CurrencyCode,
    string? InstrumentSymbol,
    string? InstrumentName,
    string? ReferenceNumber,
    DateTime? ProcessedAt,
    string? ExternalId,
    DateTime CreatedAt,
    byte[] RowVersion);
