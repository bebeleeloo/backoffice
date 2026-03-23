using System.Text.Json;
using Broker.Gateway.Api.Config;

namespace Broker.Gateway.Api.Services;

public sealed record FieldChangeDto(
    string FieldName,
    string ChangeType,
    string? OldValue,
    string? NewValue);

public sealed record EntityChangeGroupDto(
    string? RelatedEntityType,
    string? RelatedEntityId,
    string? RelatedEntityDisplayName,
    string ChangeType,
    List<FieldChangeDto> Fields);

public sealed record OperationDto(
    string OperationId,
    DateTime Timestamp,
    Guid? UserId,
    string? UserName,
    string? EntityDisplayName,
    string ChangeType,
    List<EntityChangeGroupDto> Changes);

public sealed record GlobalOperationDto(
    string OperationId,
    DateTime Timestamp,
    Guid? UserId,
    string? UserName,
    string EntityType,
    string EntityId,
    string? EntityDisplayName,
    string ChangeType,
    List<EntityChangeGroupDto> Changes);

public sealed class ConfigDiffService(ILogger<ConfigDiffService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public List<EntityChangeGroupDto> ComputeDiff(string? entityType, string? beforeJson, string? afterJson)
    {
        try
        {
            return entityType switch
            {
                "UpstreamsConfig" => DiffUpstreams(beforeJson, afterJson),
                "MenuConfig" => DiffMenu(beforeJson, afterJson),
                "EntitiesConfig" => DiffEntities(beforeJson, afterJson),
                _ => [],
            };
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to compute diff for entity type {EntityType}", entityType);
            return [];
        }
    }

    public string DetermineChangeType(string? beforeJson, string? afterJson)
    {
        var hasBefore = !string.IsNullOrEmpty(beforeJson);
        var hasAfter = !string.IsNullOrEmpty(afterJson);

        if (!hasBefore && hasAfter) return "Created";
        if (hasBefore && !hasAfter) return "Deleted";
        if (!hasBefore && !hasAfter) return "Unknown";
        return "Modified";
    }

    private List<EntityChangeGroupDto> DiffUpstreams(string? beforeJson, string? afterJson)
    {
        var before = Deserialize<Dictionary<string, UpstreamEntry>>(beforeJson) ?? [];
        var after = Deserialize<Dictionary<string, UpstreamEntry>>(afterJson) ?? [];

        var changes = new List<EntityChangeGroupDto>();
        var allKeys = new HashSet<string>(before.Keys);
        allKeys.UnionWith(after.Keys);

        foreach (var key in allKeys.OrderBy(k => k))
        {
            var hasBefore = before.TryGetValue(key, out var b);
            var hasAfter = after.TryGetValue(key, out var a);

            if (hasBefore && !hasAfter)
            {
                changes.Add(new EntityChangeGroupDto(
                    "Upstream", key, key, "Deleted",
                    [new FieldChangeDto(key, "Deleted", FormatUpstream(b!), null)]));
            }
            else if (!hasBefore && hasAfter)
            {
                changes.Add(new EntityChangeGroupDto(
                    "Upstream", key, key, "Created",
                    [new FieldChangeDto(key, "Created", null, FormatUpstream(a!))]));
            }
            else if (hasBefore && hasAfter && !UpstreamsEqual(b!, a!))
            {
                var fields = new List<FieldChangeDto>();
                if (b!.Address != a!.Address)
                    fields.Add(new FieldChangeDto("Address", "Modified", b.Address, a.Address));

                var bRoutes = string.Join(", ", b.Routes);
                var aRoutes = string.Join(", ", a.Routes);
                if (bRoutes != aRoutes)
                    fields.Add(new FieldChangeDto("Routes", "Modified", bRoutes, aRoutes));

                changes.Add(new EntityChangeGroupDto(
                    "Upstream", key, key, "Modified", fields));
            }
        }

        return changes;
    }

    private List<EntityChangeGroupDto> DiffMenu(string? beforeJson, string? afterJson)
    {
        var before = FlattenMenu(Deserialize<List<MenuItemConfig>>(beforeJson) ?? []);
        var after = FlattenMenu(Deserialize<List<MenuItemConfig>>(afterJson) ?? []);

        var changes = new List<EntityChangeGroupDto>();
        var allIds = new HashSet<string>(before.Keys);
        allIds.UnionWith(after.Keys);

        foreach (var id in allIds.OrderBy(k => k))
        {
            var hasBefore = before.TryGetValue(id, out var b);
            var hasAfter = after.TryGetValue(id, out var a);

            if (hasBefore && !hasAfter)
            {
                changes.Add(new EntityChangeGroupDto(
                    "MenuItem", id, b!.Label, "Deleted",
                    [new FieldChangeDto(id, "Deleted", FormatMenuItem(b), null)]));
            }
            else if (!hasBefore && hasAfter)
            {
                changes.Add(new EntityChangeGroupDto(
                    "MenuItem", id, a!.Label, "Created",
                    [new FieldChangeDto(id, "Created", null, FormatMenuItem(a))]));
            }
            else if (hasBefore && hasAfter && !MenuItemsEqual(b!, a!))
            {
                var fields = new List<FieldChangeDto>();
                if (b!.Label != a!.Label)
                    fields.Add(new FieldChangeDto("Label", "Modified", b.Label, a.Label));
                if (b.Icon != a.Icon)
                    fields.Add(new FieldChangeDto("Icon", "Modified", b.Icon, a.Icon));
                if (b.Path != a.Path)
                    fields.Add(new FieldChangeDto("Path", "Modified", b.Path ?? "", a.Path ?? ""));

                var bPerms = string.Join(", ", b.Permissions ?? []);
                var aPerms = string.Join(", ", a.Permissions ?? []);
                if (bPerms != aPerms)
                    fields.Add(new FieldChangeDto("Permissions", "Modified", bPerms, aPerms));

                changes.Add(new EntityChangeGroupDto(
                    "MenuItem", id, a.Label, "Modified", fields));
            }
        }

        return changes;
    }

    private List<EntityChangeGroupDto> DiffEntities(string? beforeJson, string? afterJson)
    {
        var before = ToDictionaryLast(Deserialize<List<EntityConfig>>(beforeJson) ?? [], e => e.Name);
        var after = ToDictionaryLast(Deserialize<List<EntityConfig>>(afterJson) ?? [], e => e.Name);

        var changes = new List<EntityChangeGroupDto>();
        var allNames = new HashSet<string>(before.Keys);
        allNames.UnionWith(after.Keys);

        foreach (var name in allNames.OrderBy(n => n))
        {
            var hasBefore = before.TryGetValue(name, out var b);
            var hasAfter = after.TryGetValue(name, out var a);

            if (hasBefore && !hasAfter)
            {
                changes.Add(new EntityChangeGroupDto(
                    "Entity", name, name, "Deleted",
                    [new FieldChangeDto(name, "Deleted", $"{b!.Fields.Count} fields", null)]));
            }
            else if (!hasBefore && hasAfter)
            {
                changes.Add(new EntityChangeGroupDto(
                    "Entity", name, name, "Created",
                    [new FieldChangeDto(name, "Created", null, $"{a!.Fields.Count} fields")]));
            }
            else if (hasBefore && hasAfter && !EntitiesEqual(b!, a!))
            {
                var fields = DiffEntityFields(b!, a!);
                changes.Add(new EntityChangeGroupDto(
                    "Entity", name, name, "Modified", fields));
            }
        }

        return changes;
    }

    private static List<FieldChangeDto> DiffEntityFields(EntityConfig before, EntityConfig after)
    {
        var bFields = ToDictionaryLast(before.Fields, f => f.Name);
        var aFields = ToDictionaryLast(after.Fields, f => f.Name);

        var changes = new List<FieldChangeDto>();
        var allNames = new HashSet<string>(bFields.Keys);
        allNames.UnionWith(aFields.Keys);

        foreach (var name in allNames.OrderBy(n => n))
        {
            var hasB = bFields.TryGetValue(name, out var bf);
            var hasA = aFields.TryGetValue(name, out var af);

            if (hasB && !hasA)
            {
                changes.Add(new FieldChangeDto(name, "Deleted", FormatRoles(bf!.Roles), null));
            }
            else if (!hasB && hasA)
            {
                changes.Add(new FieldChangeDto(name, "Created", null, FormatRoles(af!.Roles)));
            }
            else if (hasB && hasA)
            {
                var bRoles = FormatRoles(bf!.Roles);
                var aRoles = FormatRoles(af!.Roles);
                if (bRoles != aRoles)
                    changes.Add(new FieldChangeDto(name, "Modified", bRoles, aRoles));
            }
        }

        return changes;
    }

    private static Dictionary<string, MenuItemConfig> FlattenMenu(List<MenuItemConfig> items)
    {
        var result = new Dictionary<string, MenuItemConfig>();
        foreach (var item in items)
        {
            result[item.Id] = item;
            if (item.Children != null)
            {
                foreach (var child in item.Children)
                    result[child.Id] = child;
            }
        }
        return result;
    }

    private static string FormatUpstream(UpstreamEntry entry)
        => $"{entry.Address} — {entry.Routes.Count} routes";

    private static string FormatMenuItem(MenuItemConfig item)
        => $"{item.Label} ({item.Icon}, {item.Path ?? "no path"})";

    private static string FormatRoles(List<string> roles)
        => string.Join(", ", roles);

    private static bool UpstreamsEqual(UpstreamEntry a, UpstreamEntry b)
        => a.Address == b.Address && a.Routes.SequenceEqual(b.Routes);

    private static bool MenuItemsEqual(MenuItemConfig a, MenuItemConfig b)
        => a.Label == b.Label
           && a.Icon == b.Icon
           && a.Path == b.Path
           && (a.Permissions ?? []).SequenceEqual(b.Permissions ?? []);

    private static bool EntitiesEqual(EntityConfig a, EntityConfig b)
    {
        if (a.Fields.Count != b.Fields.Count) return false;
        for (var i = 0; i < a.Fields.Count; i++)
        {
            if (a.Fields[i].Name != b.Fields[i].Name) return false;
            if (!a.Fields[i].Roles.SequenceEqual(b.Fields[i].Roles)) return false;
        }
        return true;
    }

    private static Dictionary<TKey, T> ToDictionaryLast<T, TKey>(IEnumerable<T> source, Func<T, TKey> keySelector)
        where TKey : notnull
    {
        var dict = new Dictionary<TKey, T>();
        foreach (var item in source)
            dict[keySelector(item)] = item;
        return dict;
    }

    private T? Deserialize<T>(string? json) where T : class
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize {Type} from JSON", typeof(T).Name);
            return null;
        }
    }
}
