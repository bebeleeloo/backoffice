using Broker.Gateway.Api.Config;

namespace Broker.Gateway.Api.Services;

public sealed class EntityConfigService
{
    private readonly ConfigLoader _config;

    public EntityConfigService(ConfigLoader config)
    {
        _config = config;
    }

    public List<EntityConfig> GetEntitiesForRole(string role)
    {
        return _config.Entities.Entities
            .Select(e => FilterEntityFields(e, role))
            .Where(e => e.Fields.Count > 0)
            .ToList();
    }

    public EntityConfig? GetEntityForRole(string entityName, string role)
    {
        var entity = _config.Entities.Entities
            .FirstOrDefault(e => string.Equals(e.Name, entityName, StringComparison.OrdinalIgnoreCase));

        if (entity == null) return null;

        var filtered = FilterEntityFields(entity, role);
        return filtered.Fields.Count > 0 ? filtered : null;
    }

    private static EntityConfig FilterEntityFields(EntityConfig entity, string role)
    {
        var visibleFields = entity.Fields
            .Where(f => f.Roles.Contains("*") || f.Roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .ToList();

        return new EntityConfig
        {
            Name = entity.Name,
            Fields = visibleFields
        };
    }
}
