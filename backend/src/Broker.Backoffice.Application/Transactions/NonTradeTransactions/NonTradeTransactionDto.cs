using Broker.Backoffice.Domain.Transactions;

namespace Broker.Backoffice.Application.Transactions.NonTradeTransactions;

public sealed record NonTradeTransactionDto(
    Guid Id,
    Guid? OrderId,
    string? OrderNumber,
    string? AccountNumber,
    string TransactionNumber,
    TransactionStatus Status,
    DateTime TransactionDate,
    string? Comment,
    string? ExternalId,
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

public sealed record NonTradeTransactionListItemDto(
    Guid Id,
    string? OrderNumber,
    string? AccountNumber,
    string TransactionNumber,
    TransactionStatus Status,
    DateTime TransactionDate,
    decimal Amount,
    string CurrencyCode,
    string? InstrumentSymbol,
    string? InstrumentName,
    string? ReferenceNumber,
    DateTime? ProcessedAt,
    string? ExternalId,
    DateTime CreatedAt,
    byte[] RowVersion);
