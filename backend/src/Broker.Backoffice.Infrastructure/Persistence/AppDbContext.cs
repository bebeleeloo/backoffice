using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Accounts;
using Broker.Backoffice.Domain.Audit;
using Broker.Backoffice.Domain.Clients;
using Broker.Backoffice.Domain.Countries;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Domain.Instruments;
using Broker.Backoffice.Domain.Orders;
using Broker.Backoffice.Domain.Transactions;
using Broker.Backoffice.Infrastructure.Persistence.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Broker.Backoffice.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    private readonly ICurrentUser? _currentUser;
    private readonly IDateTimeProvider? _dateTimeProvider;
    private readonly IChangeTrackingContext? _changeTrackingContext;
    private bool _suppressChangeTracking;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
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
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientAddress> ClientAddresses => Set<ClientAddress>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<InvestmentProfile> InvestmentProfiles => Set<InvestmentProfile>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountHolder> AccountHolders => Set<AccountHolder>();
    public DbSet<Clearer> Clearers => Set<Clearer>();
    public DbSet<TradePlatform> TradePlatforms => Set<TradePlatform>();
    public DbSet<Instrument> Instruments => Set<Instrument>();
    public DbSet<Exchange> Exchanges => Set<Exchange>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<TradeOrder> TradeOrders => Set<TradeOrder>();
    public DbSet<NonTradeOrder> NonTradeOrders => Set<NonTradeOrder>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TradeTransaction> TradeTransactions => Set<TradeTransaction>();
    public DbSet<NonTradeTransaction> NonTradeTransactions => Set<NonTradeTransaction>();
    public DbSet<EntityChange> EntityChanges => Set<EntityChange>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
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
            if (config is null)
                continue;

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

        return DeduplicateReplacedEntities(changes);
    }

    /// <summary>
    /// Detects "clear all + re-add" patterns for child entities (e.g. addresses, holders).
    /// When a Deleted entity and a Created entity of the same type under the same parent
    /// have identical field values, they cancel out (no real change).
    /// When they partially differ, they're converted to a single "Modified" entry.
    /// </summary>
    private static List<EntityChange> DeduplicateReplacedEntities(List<EntityChange> changes)
    {
        // Group child entity changes by context: same operation + same parent + same child type
        var contextGroups = changes
            .Where(c => c.RelatedEntityType != null)
            .GroupBy(c => (c.OperationId, c.EntityType, c.EntityId, c.RelatedEntityType));

        var removals = new HashSet<EntityChange>();
        var additions = new List<EntityChange>();

        foreach (var context in contextGroups)
        {
            // Group by RelatedEntityId to get per-entity field sets
            var entityGroups = context.GroupBy(c => c.RelatedEntityId!).ToList();

            // Pass 0: handle "mixed" groups — same RelatedEntityId has both Created and Deleted
            // (happens with composite-key entities like AccountHolder where clear+re-add
            // produces the same composite key for both the deleted and created entity)
            foreach (var group in entityGroups)
            {
                var deleted = group.Where(c => c.ChangeType == "Deleted").ToList();
                var created = group.Where(c => c.ChangeType == "Created").ToList();
                if (deleted.Count == 0 || created.Count == 0)
                    continue;

                // This entity was replaced in-place — remove all raw entries
                foreach (var c in group) removals.Add(c);

                // Emit "Modified" entries only for fields that actually changed
                var delFields = deleted.ToDictionary(c => c.FieldName, c => c.OldValue);
                var creFields = created.ToDictionary(c => c.FieldName, c => c.NewValue);
                var template = deleted.First();
                var createdTemplate = created.First();

                foreach (var kv in delFields)
                {
                    if (!creFields.TryGetValue(kv.Key, out var newVal))
                        continue;
                    if (newVal == kv.Value)
                        continue;

                    additions.Add(new EntityChange
                    {
                        Id = Guid.NewGuid(),
                        OperationId = template.OperationId,
                        EntityType = template.EntityType,
                        EntityId = template.EntityId,
                        EntityDisplayName = createdTemplate.EntityDisplayName ?? template.EntityDisplayName,
                        RelatedEntityType = template.RelatedEntityType,
                        RelatedEntityId = template.RelatedEntityId,
                        RelatedEntityDisplayName = createdTemplate.RelatedEntityDisplayName ?? template.RelatedEntityDisplayName,
                        ChangeType = "Modified",
                        FieldName = kv.Key,
                        OldValue = kv.Value,
                        NewValue = newVal,
                        UserId = template.UserId,
                        UserName = template.UserName,
                        Timestamp = template.Timestamp
                    });
                }
            }

            var deletedEntities = entityGroups
                .Where(g => g.All(c => c.ChangeType == "Deleted") && !g.Any(c => removals.Contains(c)))
                .ToList();
            var createdEntities = entityGroups
                .Where(g => g.All(c => c.ChangeType == "Created") && !g.Any(c => removals.Contains(c)))
                .ToList();

            if (deletedEntities.Count == 0 || createdEntities.Count == 0)
                continue;

            var usedCreatedKeys = new HashSet<string>();

            // Pass 1: find exact matches (identical field values) and remove both
            foreach (var del in deletedEntities)
            {
                var delFields = del.ToDictionary(c => c.FieldName, c => c.OldValue);

                var exactMatch = createdEntities.FirstOrDefault(cr =>
                {
                    if (usedCreatedKeys.Contains(cr.Key!)) return false;
                    var crFields = cr.ToDictionary(c => c.FieldName, c => c.NewValue);
                    return delFields.Count == crFields.Count &&
                           delFields.All(kv => crFields.TryGetValue(kv.Key, out var v) && v == kv.Value);
                });

                if (exactMatch != null)
                {
                    foreach (var c in del) removals.Add(c);
                    foreach (var c in exactMatch) removals.Add(c);
                    usedCreatedKeys.Add(exactMatch.Key!);
                }
            }

            // Pass 2: for remaining unmatched, if counts match, try partial matching
            var remainingDeleted = deletedEntities
                .Where(d => !d.Any(c => removals.Contains(c)))
                .ToList();
            var remainingCreated = createdEntities
                .Where(c => !usedCreatedKeys.Contains(c.Key!))
                .ToList();

            if (remainingDeleted.Count == 0 || remainingCreated.Count == 0)
                continue;

            // Match each remaining deleted with the created entity that has highest field overlap
            var usedCreatedForPartial = new HashSet<string>();

            foreach (var del in remainingDeleted)
            {
                var delFields = del.ToDictionary(c => c.FieldName, c => c.OldValue);

                IGrouping<string, EntityChange>? bestMatch = null;
                int bestOverlap = 0;

                foreach (var cre in remainingCreated)
                {
                    if (usedCreatedForPartial.Contains(cre.Key!)) continue;
                    var creFields = cre.ToDictionary(c => c.FieldName, c => c.NewValue);

                    int overlap = delFields.Count(kv =>
                        creFields.TryGetValue(kv.Key, out var v) && v == kv.Value);

                    if (overlap > bestOverlap)
                    {
                        bestOverlap = overlap;
                        bestMatch = cre;
                    }
                }

                // Only merge if at least half the fields match
                if (bestMatch == null || bestOverlap < delFields.Count / 2)
                    continue;

                var matchedCreFields = bestMatch.ToDictionary(c => c.FieldName, c => c.NewValue);

                // Remove the raw Delete+Add entries
                foreach (var c in del) removals.Add(c);
                foreach (var c in bestMatch) removals.Add(c);
                usedCreatedForPartial.Add(bestMatch.Key!);

                // Add "Modified" entries only for fields that actually differ
                var template = del.First();
                var matchedTemplate = bestMatch.First();
                foreach (var kv in delFields)
                {
                    if (!matchedCreFields.TryGetValue(kv.Key, out var newVal))
                        continue;
                    if (newVal == kv.Value)
                        continue;

                    additions.Add(new EntityChange
                    {
                        Id = Guid.NewGuid(),
                        OperationId = template.OperationId,
                        EntityType = template.EntityType,
                        EntityId = template.EntityId,
                        EntityDisplayName = matchedTemplate.EntityDisplayName ?? template.EntityDisplayName,
                        RelatedEntityType = template.RelatedEntityType,
                        RelatedEntityId = template.RelatedEntityId,
                        RelatedEntityDisplayName = matchedTemplate.RelatedEntityDisplayName ?? template.RelatedEntityDisplayName,
                        ChangeType = "Modified",
                        FieldName = kv.Key,
                        OldValue = kv.Value,
                        NewValue = newVal,
                        UserId = template.UserId,
                        UserName = template.UserName,
                        Timestamp = template.Timestamp
                    });
                }

                // Check for new fields in created that weren't in deleted
                foreach (var kv in matchedCreFields)
                {
                    if (delFields.ContainsKey(kv.Key))
                        continue;
                    if (kv.Value is null)
                        continue;

                    additions.Add(new EntityChange
                    {
                        Id = Guid.NewGuid(),
                        OperationId = template.OperationId,
                        EntityType = template.EntityType,
                        EntityId = template.EntityId,
                        EntityDisplayName = matchedTemplate.EntityDisplayName ?? template.EntityDisplayName,
                        RelatedEntityType = template.RelatedEntityType,
                        RelatedEntityId = template.RelatedEntityId,
                        RelatedEntityDisplayName = matchedTemplate.RelatedEntityDisplayName ?? template.RelatedEntityDisplayName,
                        ChangeType = "Modified",
                        FieldName = kv.Key,
                        OldValue = null,
                        NewValue = kv.Value,
                        UserId = template.UserId,
                        UserName = template.UserName,
                        Timestamp = template.Timestamp
                    });
                }
            }
        }

        if (removals.Count == 0)
            return changes;

        var result = changes.Where(c => !removals.Contains(c)).ToList();
        result.AddRange(additions);
        return result;
    }

    private void CaptureCreated(
        List<EntityChange> changes, EntityEntry entry, TrackedEntityConfig config,
        Guid operationId, DateTime timestamp, string? userId, string? userName)
    {
        var parentInfos = ResolveParentInfos(entry, config);

        foreach (var prop in entry.Properties)
        {
            if (ShouldSkipProperty(prop, config))
                continue;
            var newValue = ValueToString(prop.CurrentValue);
            if (newValue is null)
                continue;
            newValue = TryResolveReferenceValue(prop.Metadata.Name, newValue) ?? newValue;

            foreach (var parent in parentInfos)
            {
                changes.Add(new EntityChange
                {
                    Id = Guid.NewGuid(),
                    OperationId = operationId,
                    EntityType = parent.EntityType,
                    EntityId = parent.EntityId,
                    EntityDisplayName = parent.EntityDisplayName,
                    RelatedEntityType = parent.RelatedEntityType,
                    RelatedEntityId = parent.RelatedEntityId,
                    RelatedEntityDisplayName = parent.RelatedEntityDisplayName,
                    ChangeType = "Created",
                    FieldName = prop.Metadata.Name,
                    OldValue = null,
                    NewValue = newValue,
                    UserId = userId,
                    UserName = userName,
                    Timestamp = timestamp
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
            if (ShouldSkipProperty(prop, config))
                continue;
            if (!prop.IsModified)
                continue;

            var oldValue = ValueToString(prop.OriginalValue);
            var newValue = ValueToString(prop.CurrentValue);
            if (oldValue == newValue)
                continue;
            oldValue = TryResolveReferenceValue(prop.Metadata.Name, oldValue) ?? oldValue;
            newValue = TryResolveReferenceValue(prop.Metadata.Name, newValue) ?? newValue;

            foreach (var parent in parentInfos)
            {
                changes.Add(new EntityChange
                {
                    Id = Guid.NewGuid(),
                    OperationId = operationId,
                    EntityType = parent.EntityType,
                    EntityId = parent.EntityId,
                    EntityDisplayName = parent.EntityDisplayName,
                    RelatedEntityType = parent.RelatedEntityType,
                    RelatedEntityId = parent.RelatedEntityId,
                    RelatedEntityDisplayName = parent.RelatedEntityDisplayName,
                    ChangeType = "Modified",
                    FieldName = prop.Metadata.Name,
                    OldValue = oldValue,
                    NewValue = newValue,
                    UserId = userId,
                    UserName = userName,
                    Timestamp = timestamp
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
            if (ShouldSkipProperty(prop, config))
                continue;
            var oldValue = ValueToString(prop.OriginalValue);
            if (oldValue is null)
                continue;
            oldValue = TryResolveReferenceValue(prop.Metadata.Name, oldValue) ?? oldValue;

            foreach (var parent in parentInfos)
            {
                changes.Add(new EntityChange
                {
                    Id = Guid.NewGuid(),
                    OperationId = operationId,
                    EntityType = parent.EntityType,
                    EntityId = parent.EntityId,
                    EntityDisplayName = parent.EntityDisplayName,
                    RelatedEntityType = parent.RelatedEntityType,
                    RelatedEntityId = parent.RelatedEntityId,
                    RelatedEntityDisplayName = parent.RelatedEntityDisplayName,
                    ChangeType = "Deleted",
                    FieldName = prop.Metadata.Name,
                    OldValue = oldValue,
                    NewValue = null,
                    UserId = userId,
                    UserName = userName,
                    Timestamp = timestamp
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
            var relatedDisplayName = ResolveEntityDisplayName(entry, config, mapping);
            result.Add(new ParentInfo(mapping.ParentEntityTypeName, parentId, parentDisplayName,
                config.EntityTypeName, relatedId, relatedDisplayName));
        }

        return result;
    }

    private static string GetEntityId(EntityEntry entry, TrackedEntityConfig config)
    {
        // Check if entity has Id property (Entity subclasses)
        var idProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
        if (idProp is not null)
            return ValueToString(idProp.CurrentValue ?? idProp.OriginalValue) ?? "";

        // Composite key — serialize all parent FK props + key fields
        var parts = config.ParentMappings
            .Select(m => $"{m.ForeignKeyProperty}:{GetPropertyStringValue(entry, m.ForeignKeyProperty)}")
            .ToList();

        // Add non-FK key fields (e.g., Role for AccountHolder)
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

        // Skip excluded properties
        if (config.ExcludedProperties.Contains(name))
            return true;

        // Skip byte[] properties (RowVersion, etc.)
        if (prop.Metadata.ClrType == typeof(byte[]))
            return true;

        // Skip navigation-related shadow properties that aren't explicitly mapped
        if (prop.Metadata.IsShadowProperty())
            return true;

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
        byte[] => null, // skip binary data
        Guid g => g.ToString(),
        _ => value.ToString()
    };

    private string? ResolveEntityDisplayName(EntityEntry entry, TrackedEntityConfig config, ParentMapping? parentMapping = null)
    {
        return config.EntityTypeName switch
        {
            "Client" => BuildClientDisplayName(entry),
            "Account" => GetPropertyStringValueOrNull(entry, "Number"),
            "Instrument" => GetPropertyStringValueOrNull(entry, "Symbol"),
            "User" => GetPropertyStringValueOrNull(entry, "Username"),
            "Role" => GetPropertyStringValueOrNull(entry, "Name"),
            "ClientAddress" => BuildAddressDisplayName(entry),
            "InvestmentProfile" => null,
            "AccountHolder" => BuildAccountHolderDisplayName(entry, parentMapping),
            "UserRole" => ResolveRoleName(entry),
            "RolePermission" => ResolvePermissionCode(entry),
            "Order" => GetPropertyStringValueOrNull(entry, "OrderNumber"),
            "TradeOrder" => null,
            "NonTradeOrder" => null,
            "Transaction" => GetPropertyStringValueOrNull(entry, "TransactionNumber"),
            "TradeTransaction" => null,
            "NonTradeTransaction" => null,
            _ => null
        };
    }

    private string? ResolveRoleName(EntityEntry entry)
    {
        var roleId = entry.Property("RoleId").CurrentValue ?? entry.Property("RoleId").OriginalValue;
        if (roleId is not Guid id) return null;
        return ResolveRoleName(id);
    }

    private string? ResolveRoleName(Guid id) =>
        Set<Role>().Local.FirstOrDefault(r => r.Id == id)?.Name
        ?? Roles.AsNoTracking().Where(r => r.Id == id).Select(r => r.Name).FirstOrDefault();

    private string? ResolvePermissionCode(EntityEntry entry)
    {
        var permId = entry.Property("PermissionId").CurrentValue ?? entry.Property("PermissionId").OriginalValue;
        if (permId is not Guid id) return null;
        return ResolvePermissionCode(id);
    }

    private string? ResolvePermissionCode(Guid id) =>
        Set<Permission>().Local.FirstOrDefault(p => p.Id == id)?.Code
        ?? Permissions.AsNoTracking().Where(p => p.Id == id).Select(p => p.Code).FirstOrDefault();

    private string? ResolveCountryName(Guid id) =>
        Set<Country>().Local.FirstOrDefault(c => c.Id == id)?.Name
        ?? Countries.AsNoTracking().Where(c => c.Id == id).Select(c => c.Name).FirstOrDefault();

    private string? ResolveClearerName(Guid id) =>
        Set<Clearer>().Local.FirstOrDefault(c => c.Id == id)?.Name
        ?? Clearers.AsNoTracking().Where(c => c.Id == id).Select(c => c.Name).FirstOrDefault();

    private string? ResolveTradePlatformName(Guid id) =>
        Set<TradePlatform>().Local.FirstOrDefault(t => t.Id == id)?.Name
        ?? TradePlatforms.AsNoTracking().Where(t => t.Id == id).Select(t => t.Name).FirstOrDefault();

    private string? ResolveExchangeName(Guid id) =>
        Set<Exchange>().Local.FirstOrDefault(e => e.Id == id)?.Name
        ?? Exchanges.AsNoTracking().Where(e => e.Id == id).Select(e => e.Name).FirstOrDefault();

    private string? ResolveCurrencyCode(Guid id) =>
        Set<Currency>().Local.FirstOrDefault(c => c.Id == id)?.Code
        ?? Currencies.AsNoTracking().Where(c => c.Id == id).Select(c => c.Code).FirstOrDefault();

    private string? ResolveAccountNumber(Guid id) =>
        Set<Account>().Local.FirstOrDefault(a => a.Id == id)?.Number
        ?? Accounts.AsNoTracking().Where(a => a.Id == id).Select(a => a.Number).FirstOrDefault();

    private string? ResolveInstrumentSymbol(Guid id) =>
        Set<Instrument>().Local.FirstOrDefault(i => i.Id == id)?.Symbol
        ?? Instruments.AsNoTracking().Where(i => i.Id == id).Select(i => i.Symbol).FirstOrDefault();

    /// <summary>
    /// Resolves FK GUID values to human-readable names for known reference fields.
    /// Returns null if the field is not a reference field or resolution fails.
    /// </summary>
    private string? TryResolveReferenceValue(string fieldName, string? rawValue)
    {
        if (rawValue is null || !Guid.TryParse(rawValue, out var id))
            return null;

        return fieldName switch
        {
            "ResidenceCountryId" or "CitizenshipCountryId" or "CountryId" => ResolveCountryName(id),
            "ClearerId" => ResolveClearerName(id),
            "TradePlatformId" => ResolveTradePlatformName(id),
            "ExchangeId" => ResolveExchangeName(id),
            "CurrencyId" => ResolveCurrencyCode(id),
            "PermissionId" => ResolvePermissionCode(id),
            "RoleId" => ResolveRoleName(id),
            "AccountId" => ResolveAccountNumber(id),
            "InstrumentId" => ResolveInstrumentSymbol(id),
            _ => null
        };
    }

    private static string? BuildClientDisplayName(EntityEntry entry)
    {
        var clientType = GetPropertyStringValueOrNull(entry, "ClientType");
        if (clientType == "Corporate")
            return GetPropertyStringValueOrNull(entry, "CompanyName");

        var first = GetPropertyStringValueOrNull(entry, "FirstName") ?? "";
        var last = GetPropertyStringValueOrNull(entry, "LastName") ?? "";
        var name = $"{first} {last}".Trim();
        return name.Length > 0 ? name : null;
    }

    private static string? BuildAddressDisplayName(EntityEntry entry)
    {
        var type = GetPropertyStringValueOrNull(entry, "Type");
        var line1 = GetPropertyStringValueOrNull(entry, "Line1");
        var city = GetPropertyStringValueOrNull(entry, "City");

        var parts = new List<string>();
        if (line1 != null) parts.Add(line1);
        if (city != null) parts.Add(city);

        var detail = string.Join(", ", parts);
        if (string.IsNullOrEmpty(detail))
            return type;
        return type != null ? $"{type}, {detail}" : detail;
    }

    private string? BuildAccountHolderDisplayName(EntityEntry entry, ParentMapping? parentMapping)
    {
        var role = GetPropertyStringValueOrNull(entry, "Role");

        string? context = null;
        if (parentMapping?.ParentEntityTypeName == "Account")
        {
            // Viewing from Account history → show client name for context
            var clientId = entry.Property("ClientId").CurrentValue ?? entry.Property("ClientId").OriginalValue;
            if (clientId is Guid id) context = FindClientDisplayName(id);
        }
        else if (parentMapping?.ParentEntityTypeName == "Client")
        {
            // Viewing from Client history → show account number for context
            var accountId = entry.Property("AccountId").CurrentValue ?? entry.Property("AccountId").OriginalValue;
            if (accountId is Guid id)
                context = Set<Account>().Local.FirstOrDefault(a => a.Id == id)?.Number
                          ?? Accounts.AsNoTracking().Where(a => a.Id == id).Select(a => a.Number).FirstOrDefault();
        }

        if (role != null && context != null)
            return $"{role}, {context}";
        return context ?? role;
    }

    private string? ResolveParentDisplayName(string parentEntityTypeName, string parentId)
    {
        if (!Guid.TryParse(parentId, out var id))
            return null;

        return parentEntityTypeName switch
        {
            "Client" => FindClientDisplayName(id),
            "Account" => Set<Account>().Local.FirstOrDefault(a => a.Id == id)?.Number
                         ?? Accounts.AsNoTracking().Where(a => a.Id == id).Select(a => a.Number).FirstOrDefault(),
            "User" => Set<User>().Local.FirstOrDefault(u => u.Id == id)?.Username
                      ?? Users.AsNoTracking().Where(u => u.Id == id).Select(u => u.Username).FirstOrDefault(),
            "Role" => Set<Role>().Local.FirstOrDefault(r => r.Id == id)?.Name
                      ?? Roles.AsNoTracking().Where(r => r.Id == id).Select(r => r.Name).FirstOrDefault(),
            "Order" => Set<Order>().Local.FirstOrDefault(o => o.Id == id)?.OrderNumber
                       ?? Orders.AsNoTracking().Where(o => o.Id == id).Select(o => o.OrderNumber).FirstOrDefault(),
            "Transaction" => Set<Transaction>().Local.FirstOrDefault(t => t.Id == id)?.TransactionNumber
                             ?? Transactions.AsNoTracking().Where(t => t.Id == id).Select(t => t.TransactionNumber).FirstOrDefault(),
            _ => null
        };
    }

    private string? FindClientDisplayName(Guid id)
    {
        var local = Set<Client>().Local.FirstOrDefault(c => c.Id == id);
        if (local is not null)
        {
            return local.ClientType == ClientType.Corporate
                ? local.CompanyName
                : $"{local.FirstName} {local.LastName}".Trim();
        }

        return Clients.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => c.ClientType == ClientType.Corporate
                ? c.CompanyName
                : (c.FirstName + " " + c.LastName))
            .FirstOrDefault()?.Trim();
    }

    private static string? GetPropertyStringValueOrNull(EntityEntry entry, string propertyName)
    {
        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == propertyName);
        if (prop is null) return null;
        return ValueToString(prop.CurrentValue ?? prop.OriginalValue);
    }

    private sealed record ParentInfo(
        string EntityType,
        string EntityId,
        string? EntityDisplayName,
        string? RelatedEntityType,
        string? RelatedEntityId,
        string? RelatedEntityDisplayName);
}
