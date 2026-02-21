using Broker.Backoffice.Domain.Accounts;

namespace Broker.Backoffice.Application.Accounts;

public sealed record AccountDto(
    Guid Id,
    string Number,
    Guid? ClearerId,
    string? ClearerName,
    Guid? TradePlatformId,
    string? TradePlatformName,
    AccountStatus Status,
    AccountType AccountType,
    MarginType MarginType,
    OptionLevel OptionLevel,
    Tariff Tariff,
    DeliveryType? DeliveryType,
    DateTime? OpenedAt,
    DateTime? ClosedAt,
    string? Comment,
    string? ExternalId,
    DateTime CreatedAt,
    byte[] RowVersion,
    IReadOnlyList<AccountHolderDto> Holders);

public sealed record AccountHolderDto(
    Guid ClientId,
    string ClientDisplayName,
    HolderRole Role,
    bool IsPrimary,
    DateTime AddedAt);

public sealed record AccountListItemDto(
    Guid Id,
    string Number,
    string? ClearerName,
    string? TradePlatformName,
    AccountStatus Status,
    AccountType AccountType,
    MarginType MarginType,
    OptionLevel OptionLevel,
    Tariff Tariff,
    DeliveryType? DeliveryType,
    DateTime? OpenedAt,
    DateTime? ClosedAt,
    string? ExternalId,
    DateTime CreatedAt,
    byte[] RowVersion,
    int HolderCount);
