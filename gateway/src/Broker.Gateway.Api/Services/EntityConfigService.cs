using Broker.Gateway.Api.Config;

namespace Broker.Gateway.Api.Services;

public sealed class EntityConfigService
{
    private readonly ConfigLoader _config;

    public EntityConfigService(ConfigLoader config)
    {
        _config = config;
    }

    public List<EntityConfig> GetEntitiesForRole(IReadOnlyList<string> roles)
    {
        return _config.Entities.Entities
            .Select(e => FilterEntityFields(e, roles))
            .Where(e => e.Fields.Count > 0)
            .ToList();
    }

    public EntityConfig? GetEntityForRole(string entityName, IReadOnlyList<string> roles)
    {
        var entity = _config.Entities.Entities
            .FirstOrDefault(e => string.Equals(e.Name, entityName, StringComparison.OrdinalIgnoreCase));

        if (entity == null) return null;

        var filtered = FilterEntityFields(entity, roles);
        return filtered.Fields.Count > 0 ? filtered : null;
    }

    private static EntityConfig FilterEntityFields(EntityConfig entity, IReadOnlyList<string> roles)
    {
        if (roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
            return entity;

        var visibleFields = entity.Fields
            .Where(f => f.Roles.Contains("*") || f.Roles.Any(r => roles.Contains(r, StringComparer.OrdinalIgnoreCase)))
            .ToList();

        return new EntityConfig
        {
            Name = entity.Name,
            Fields = visibleFields
        };
    }
}
