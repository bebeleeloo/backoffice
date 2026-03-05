using Broker.Auth.Domain.Identity;

namespace Broker.Auth.Infrastructure.Persistence.ChangeTracking;

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
        [typeof(User)] = new TrackedEntityConfig
        {
            EntityTypeName = "User",
            ClrType = typeof(User),
            IsRoot = true,
            ExcludedProperties = [..AuditableExcluded, "PasswordHash", "Photo", "PhotoContentType"]
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
