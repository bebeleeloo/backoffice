using Broker.Backoffice.Domain.Accounts;
using Broker.Backoffice.Domain.Clients;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Domain.Instruments;

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
        [typeof(User)] = new TrackedEntityConfig
        {
            EntityTypeName = "User",
            ClrType = typeof(User),
            IsRoot = true,
            ExcludedProperties = [..AuditableExcluded, "PasswordHash"]
        },
        [typeof(UserRole)] = new TrackedEntityConfig
        {
            EntityTypeName = "UserRole",
            ClrType = typeof(UserRole),
            ParentMappings = [new() { ParentEntityTypeName = "User", ForeignKeyProperty = "UserId" }],
            ExcludedProperties = ["Id", "UserId", "User", "Role", "CreatedAt", "CreatedBy"]
        },
        [typeof(Role)] = new TrackedEntityConfig
        {
            EntityTypeName = "Role",
            ClrType = typeof(Role),
            IsRoot = true,
            ExcludedProperties = [..AuditableExcluded]
        },
        [typeof(RolePermission)] = new TrackedEntityConfig
        {
            EntityTypeName = "RolePermission",
            ClrType = typeof(RolePermission),
            ParentMappings = [new() { ParentEntityTypeName = "Role", ForeignKeyProperty = "RoleId" }],
            ExcludedProperties = ["Id", "RoleId", "Role", "Permission", "CreatedAt", "CreatedBy"]
        }
    };

    public static TrackedEntityConfig? GetConfig(Type entityType) =>
        Configs.GetValueOrDefault(entityType);
}
