using Broker.Auth.Application.Abstractions;
using Broker.Auth.Domain.Audit;
using Broker.Auth.Domain.Identity;
using Broker.Auth.Infrastructure.Persistence.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Broker.Auth.Infrastructure.Persistence;

public sealed class AuthDbContext : DbContext, IAuthDbContext
{
    private readonly ICurrentUser? _currentUser;
    private readonly IDateTimeProvider? _dateTimeProvider;
    private readonly IChangeTrackingContext? _changeTrackingContext;
    private bool _suppressChangeTracking;

    public AuthDbContext(
        DbContextOptions<AuthDbContext> options,
        ICurrentUser? currentUser = null,
        IDateTimeProvider? dateTimeProvider = null,
        IChangeTrackingContext? changeTrackingContext = null)
        : base(options)
    {
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _changeTrackingContext = changeTrackingContext;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermissionOverride> UserPermissionOverrides => Set<UserPermissionOverride>();
    public DbSet<DataScope> DataScopes => Set<DataScope>();
    public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<EntityChange> EntityChanges => Set<EntityChange>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("auth");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_suppressChangeTracking)
            return await base.SaveChangesAsync(cancellationToken);

        var changeEntries = CaptureChanges();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (changeEntries.Count > 0)
        {
            _suppressChangeTracking = true;
            try
            {
                EntityChanges.AddRange(changeEntries);
                await base.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                _suppressChangeTracking = false;
            }
        }

        return result;
    }

    private List<EntityChange> CaptureChanges()
    {
        var changes = new List<EntityChange>();
        var operationId = _changeTrackingContext?.OperationId ?? Guid.NewGuid();
        var timestamp = _dateTimeProvider?.UtcNow ?? DateTime.UtcNow;
        var userId = _currentUser?.UserId;
        var userName = _currentUser?.FullName ?? _currentUser?.UserName;

        foreach (var entry in ChangeTracker.Entries())
        {
            var config = EntityTrackingRegistry.GetConfig(entry.Entity.GetType());
            if (config is null) continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    CaptureCreated(changes, entry, config, operationId, timestamp, userId, userName);
                    break;
                case EntityState.Modified:
                    CaptureModified(changes, entry, config, operationId, timestamp, userId, userName);
                    break;
                case EntityState.Deleted:
                    CaptureDeleted(changes, entry, config, operationId, timestamp, userId, userName);
                    break;
            }
        }

        return changes;
    }

    private void CaptureCreated(
        List<EntityChange> changes, EntityEntry entry, TrackedEntityConfig config,
        Guid operationId, DateTime timestamp, string? userId, string? userName)
    {
        var parentInfos = ResolveParentInfos(entry, config);
        foreach (var prop in entry.Properties)
        {
            if (ShouldSkipProperty(prop, config)) continue;
            var newValue = ValueToString(prop.CurrentValue);
            if (newValue is null) continue;
            newValue = TryResolveReferenceValue(prop.Metadata.Name, newValue) ?? newValue;
            foreach (var parent in parentInfos)
            {
                changes.Add(new EntityChange
                {
                    Id = Guid.NewGuid(), OperationId = operationId,
                    EntityType = parent.EntityType, EntityId = parent.EntityId,
                    EntityDisplayName = parent.EntityDisplayName,
                    RelatedEntityType = parent.RelatedEntityType,
                    RelatedEntityId = parent.RelatedEntityId,
                    RelatedEntityDisplayName = parent.RelatedEntityDisplayName,
                    ChangeType = "Created", FieldName = prop.Metadata.Name,
                    OldValue = null, NewValue = newValue,
                    UserId = userId, UserName = userName, Timestamp = timestamp
                });
            }
        }
    }

    private void CaptureModified(
        List<EntityChange> changes, EntityEntry entry, TrackedEntityConfig config,
        Guid operationId, DateTime timestamp, string? userId, string? userName)
    {
        var parentInfos = ResolveParentInfos(entry, config);
        foreach (var prop in entry.Properties)
        {
            if (ShouldSkipProperty(prop, config)) continue;
            if (!prop.IsModified) continue;
            var oldValue = ValueToString(prop.OriginalValue);
            var newValue = ValueToString(prop.CurrentValue);
            if (oldValue == newValue) continue;
            oldValue = TryResolveReferenceValue(prop.Metadata.Name, oldValue) ?? oldValue;
            newValue = TryResolveReferenceValue(prop.Metadata.Name, newValue) ?? newValue;
            foreach (var parent in parentInfos)
            {
                changes.Add(new EntityChange
                {
                    Id = Guid.NewGuid(), OperationId = operationId,
                    EntityType = parent.EntityType, EntityId = parent.EntityId,
                    EntityDisplayName = parent.EntityDisplayName,
                    RelatedEntityType = parent.RelatedEntityType,
                    RelatedEntityId = parent.RelatedEntityId,
                    RelatedEntityDisplayName = parent.RelatedEntityDisplayName,
                    ChangeType = "Modified", FieldName = prop.Metadata.Name,
                    OldValue = oldValue, NewValue = newValue,
                    UserId = userId, UserName = userName, Timestamp = timestamp
                });
            }
        }
    }

    private void CaptureDeleted(
        List<EntityChange> changes, EntityEntry entry, TrackedEntityConfig config,
        Guid operationId, DateTime timestamp, string? userId, string? userName)
    {
        var parentInfos = ResolveParentInfos(entry, config);
        foreach (var prop in entry.Properties)
        {
            if (ShouldSkipProperty(prop, config)) continue;
            var oldValue = ValueToString(prop.OriginalValue);
            if (oldValue is null) continue;
            oldValue = TryResolveReferenceValue(prop.Metadata.Name, oldValue) ?? oldValue;
            foreach (var parent in parentInfos)
            {
                changes.Add(new EntityChange
                {
                    Id = Guid.NewGuid(), OperationId = operationId,
                    EntityType = parent.EntityType, EntityId = parent.EntityId,
                    EntityDisplayName = parent.EntityDisplayName,
                    RelatedEntityType = parent.RelatedEntityType,
                    RelatedEntityId = parent.RelatedEntityId,
                    RelatedEntityDisplayName = parent.RelatedEntityDisplayName,
                    ChangeType = "Deleted", FieldName = prop.Metadata.Name,
                    OldValue = oldValue, NewValue = null,
                    UserId = userId, UserName = userName, Timestamp = timestamp
                });
            }
        }
    }

    private List<ParentInfo> ResolveParentInfos(EntityEntry entry, TrackedEntityConfig config)
    {
        if (config.IsRoot)
        {
            var id = GetPropertyStringValue(entry, "Id");
            var displayName = ResolveEntityDisplayName(entry, config);
            return [new ParentInfo(config.EntityTypeName, id, displayName, null, null, null)];
        }

        var relatedId = GetEntityId(entry, config);
        var result = new List<ParentInfo>();
        foreach (var mapping in config.ParentMappings)
        {
            var parentId = GetPropertyStringValue(entry, mapping.ForeignKeyProperty);
            var parentDisplayName = ResolveParentDisplayName(mapping.ParentEntityTypeName, parentId);
            var relatedDisplayName = ResolveEntityDisplayName(entry, config);
            result.Add(new ParentInfo(mapping.ParentEntityTypeName, parentId, parentDisplayName,
                config.EntityTypeName, relatedId, relatedDisplayName));
        }
        return result;
    }

    private static string GetEntityId(EntityEntry entry, TrackedEntityConfig config)
    {
        var idProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
        if (idProp is not null)
            return ValueToString(idProp.CurrentValue ?? idProp.OriginalValue) ?? "";

        var parts = config.ParentMappings
            .Select(m => $"{m.ForeignKeyProperty}:{GetPropertyStringValue(entry, m.ForeignKeyProperty)}")
            .ToList();
        var fkNames = config.ParentMappings.Select(m => m.ForeignKeyProperty).ToHashSet();
        foreach (var keyProp in entry.Metadata.FindPrimaryKey()?.Properties ?? [])
        {
            if (!fkNames.Contains(keyProp.Name))
            {
                var val = entry.Property(keyProp.Name).CurrentValue ?? entry.Property(keyProp.Name).OriginalValue;
                parts.Add($"{keyProp.Name}:{ValueToString(val)}");
            }
        }
        return string.Join("|", parts);
    }

    private static string GetPropertyStringValue(EntityEntry entry, string propertyName)
    {
        var prop = entry.Property(propertyName);
        return ValueToString(prop.CurrentValue ?? prop.OriginalValue) ?? "";
    }

    private static bool ShouldSkipProperty(PropertyEntry prop, TrackedEntityConfig config)
    {
        var name = prop.Metadata.Name;
        if (config.ExcludedProperties.Contains(name)) return true;
        if (prop.Metadata.ClrType == typeof(byte[])) return true;
        if (prop.Metadata.IsShadowProperty()) return true;
        return false;
    }

    private static string? ValueToString(object? value) => value switch
    {
        null => null,
        Enum e => e.ToString(),
        DateTime dt => dt.ToString("O"),
        DateTimeOffset dto => dto.ToString("O"),
        DateOnly d => d.ToString("O"),
        bool b => b ? "True" : "False",
        byte[] => null,
        Guid g => g.ToString(),
        _ => value.ToString()
    };

    private string? ResolveEntityDisplayName(EntityEntry entry, TrackedEntityConfig config)
    {
        return config.EntityTypeName switch
        {
            "User" => GetPropertyStringValueOrNull(entry, "Username"),
            "Role" => GetPropertyStringValueOrNull(entry, "Name"),
            "UserRole" => ResolveRoleName(entry),
            "RolePermission" => ResolvePermissionCode(entry),
            _ => null
        };
    }

    private string? ResolveRoleName(EntityEntry entry)
    {
        var roleId = entry.Property("RoleId").CurrentValue ?? entry.Property("RoleId").OriginalValue;
        if (roleId is not Guid id) return null;
        return Set<Role>().Local.FirstOrDefault(r => r.Id == id)?.Name
            ?? Roles.AsNoTracking().Where(r => r.Id == id).Select(r => r.Name).FirstOrDefault();
    }

    private string? ResolvePermissionCode(EntityEntry entry)
    {
        var permId = entry.Property("PermissionId").CurrentValue ?? entry.Property("PermissionId").OriginalValue;
        if (permId is not Guid id) return null;
        return Set<Permission>().Local.FirstOrDefault(p => p.Id == id)?.Code
            ?? Permissions.AsNoTracking().Where(p => p.Id == id).Select(p => p.Code).FirstOrDefault();
    }

    private string? TryResolveReferenceValue(string fieldName, string? rawValue)
    {
        if (rawValue is null || !Guid.TryParse(rawValue, out var id)) return null;
        return fieldName switch
        {
            "PermissionId" => ResolvePermissionCode(id),
            "RoleId" => ResolveRoleName(id),
            _ => null
        };
    }

    private string? ResolveRoleName(Guid id) =>
        Set<Role>().Local.FirstOrDefault(r => r.Id == id)?.Name
        ?? Roles.AsNoTracking().Where(r => r.Id == id).Select(r => r.Name).FirstOrDefault();

    private string? ResolvePermissionCode(Guid id) =>
        Set<Permission>().Local.FirstOrDefault(p => p.Id == id)?.Code
        ?? Permissions.AsNoTracking().Where(p => p.Id == id).Select(p => p.Code).FirstOrDefault();

    private string? ResolveParentDisplayName(string parentEntityTypeName, string parentId)
    {
        if (!Guid.TryParse(parentId, out var id)) return null;
        return parentEntityTypeName switch
        {
            "User" => Set<User>().Local.FirstOrDefault(u => u.Id == id)?.Username
                      ?? Users.AsNoTracking().Where(u => u.Id == id).Select(u => u.Username).FirstOrDefault(),
            "Role" => Set<Role>().Local.FirstOrDefault(r => r.Id == id)?.Name
                      ?? Roles.AsNoTracking().Where(r => r.Id == id).Select(r => r.Name).FirstOrDefault(),
            _ => null
        };
    }

    private static string? GetPropertyStringValueOrNull(EntityEntry entry, string propertyName)
    {
        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == propertyName);
        if (prop is null) return null;
        return ValueToString(prop.CurrentValue ?? prop.OriginalValue);
    }

    private sealed record ParentInfo(
        string EntityType, string EntityId, string? EntityDisplayName,
        string? RelatedEntityType, string? RelatedEntityId, string? RelatedEntityDisplayName);
}
