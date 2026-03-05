using Broker.Backoffice.Domain.Accounts;
using Broker.Backoffice.Domain.Clients;
using Broker.Backoffice.Domain.Instruments;
using Broker.Backoffice.Domain.Orders;
using Broker.Backoffice.Domain.Transactions;

namespace Broker.Backoffice.Infrastructure.Persistence.ChangeTracking;

public sealed class ParentMapping
{
    public required string ParentEntityTypeName { get; init; }
    public required string ForeignKeyProperty { get; init; }
}

public sealed class TrackedEntityConfig
{
    public required string EntityTypeName { get; init; }
    public required Type ClrType { get; init; }
    public bool IsRoot { get; init; }
    public List<ParentMapping> ParentMappings { get; init; } = [];
    public HashSet<string> ExcludedProperties { get; init; } = [];
}

public static class EntityTrackingRegistry
{
    private static readonly HashSet<string> AuditableExcluded =
        ["RowVersion", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy"];

    private static readonly Dictionary<Type, TrackedEntityConfig> Configs = new()
    {
        [typeof(Client)] = new TrackedEntityConfig
        {
            EntityTypeName = "Client",
            ClrType = typeof(Client),
            IsRoot = true,
            ExcludedProperties = [..AuditableExcluded]
        },
        [typeof(ClientAddress)] = new TrackedEntityConfig
        {
            EntityTypeName = "ClientAddress",
            ClrType = typeof(ClientAddress),
            ParentMappings = [new() { ParentEntityTypeName = "Client", ForeignKeyProperty = "ClientId" }],
            ExcludedProperties = ["Id", "ClientId", "Client"]
        },
        [typeof(InvestmentProfile)] = new TrackedEntityConfig
        {
            EntityTypeName = "InvestmentProfile",
            ClrType = typeof(InvestmentProfile),
            ParentMappings = [new() { ParentEntityTypeName = "Client", ForeignKeyProperty = "ClientId" }],
            ExcludedProperties = ["Id", "ClientId", "Client"]
        },
        [typeof(Account)] = new TrackedEntityConfig
        {
            EntityTypeName = "Account",
            ClrType = typeof(Account),
            IsRoot = true,
            ExcludedProperties = [..AuditableExcluded]
        },
        [typeof(AccountHolder)] = new TrackedEntityConfig
        {
            EntityTypeName = "AccountHolder",
            ClrType = typeof(AccountHolder),
            ParentMappings =
            [
                new() { ParentEntityTypeName = "Account", ForeignKeyProperty = "AccountId" },
                new() { ParentEntityTypeName = "Client", ForeignKeyProperty = "ClientId" }
            ],
            ExcludedProperties = ["AccountId", "ClientId", "Account", "Client", "AddedAt"]
        },
        [typeof(Instrument)] = new TrackedEntityConfig
        {
            EntityTypeName = "Instrument",
            ClrType = typeof(Instrument),
            IsRoot = true,
            ExcludedProperties = [..AuditableExcluded]
        },
        [typeof(Order)] = new TrackedEntityConfig
        {
            EntityTypeName = "Order",
            ClrType = typeof(Order),
            IsRoot = true,
            ExcludedProperties = [..AuditableExcluded]
        },
        [typeof(TradeOrder)] = new TrackedEntityConfig
        {
            EntityTypeName = "TradeOrder",
            ClrType = typeof(TradeOrder),
            ParentMappings = [new() { ParentEntityTypeName = "Order", ForeignKeyProperty = "OrderId" }],
            ExcludedProperties = ["Order", "Instrument"]
        },
        [typeof(NonTradeOrder)] = new TrackedEntityConfig
        {
            EntityTypeName = "NonTradeOrder",
            ClrType = typeof(NonTradeOrder),
            ParentMappings = [new() { ParentEntityTypeName = "Order", ForeignKeyProperty = "OrderId" }],
            ExcludedProperties = ["Order", "Currency", "Instrument"]
        },
        [typeof(Transaction)] = new TrackedEntityConfig
        {
            EntityTypeName = "Transaction",
            ClrType = typeof(Transaction),
            IsRoot = true,
            ExcludedProperties = [..AuditableExcluded]
        },
        [typeof(TradeTransaction)] = new TrackedEntityConfig
        {
            EntityTypeName = "TradeTransaction",
            ClrType = typeof(TradeTransaction),
            ParentMappings = [new() { ParentEntityTypeName = "Transaction", ForeignKeyProperty = "TransactionId" }],
            ExcludedProperties = ["Transaction", "Instrument"]
        },
        [typeof(NonTradeTransaction)] = new TrackedEntityConfig
        {
            EntityTypeName = "NonTradeTransaction",
            ClrType = typeof(NonTradeTransaction),
            ParentMappings = [new() { ParentEntityTypeName = "Transaction", ForeignKeyProperty = "TransactionId" }],
            ExcludedProperties = ["Transaction", "Currency", "Instrument"]
        }
    };

    public static TrackedEntityConfig? GetConfig(Type entityType) =>
        Configs.GetValueOrDefault(entityType);
}
