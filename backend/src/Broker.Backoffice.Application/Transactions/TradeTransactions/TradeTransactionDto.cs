using Broker.Backoffice.Domain.Orders;
using Broker.Backoffice.Domain.Transactions;

namespace Broker.Backoffice.Application.Transactions.TradeTransactions;

public sealed record TradeTransactionDto(
    Guid Id,
    Guid? OrderId,
    string? OrderNumber,
    string? AccountNumber,
    string TransactionNumber,
    TransactionStatus Status,
    DateTime TransactionDate,
    string? Comment,
    string? ExternalId,
    Guid InstrumentId,
    string InstrumentSymbol,
    string InstrumentName,
    TradeSide Side,
    decimal Quantity,
    decimal Price,
    decimal? Commission,
    DateTime? SettlementDate,
    string? Venue,
    DateTime CreatedAt,
    byte[] RowVersion);

public sealed record TradeTransactionListItemDto(
    Guid Id,
    string? OrderNumber,
    string? AccountNumber,
    string TransactionNumber,
    TransactionStatus Status,
    DateTime TransactionDate,
    string InstrumentSymbol,
    string InstrumentName,
    TradeSide Side,
    decimal Quantity,
    decimal Price,
    decimal? Commission,
    DateTime? SettlementDate,
    string? Venue,
    string? ExternalId,
    DateTime CreatedAt,
    byte[] RowVersion);
