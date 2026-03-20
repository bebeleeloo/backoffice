namespace Broker.Gateway.Api.Config;

public sealed class EntityFieldConfig
{
    public string Name { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
}

public sealed class EntityConfig
{
    public string Name { get; set; } = string.Empty;
    public List<EntityFieldConfig> Fields { get; set; } = [];
}

public sealed class EntitiesConfig
{
    public List<EntityConfig> Entities { get; set; } = [];
}
