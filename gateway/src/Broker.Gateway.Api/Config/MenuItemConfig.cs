namespace Broker.Gateway.Api.Config;

public sealed class MenuItemConfig
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string? Path { get; set; }
    public List<string>? Permissions { get; set; }
    public List<MenuItemConfig>? Children { get; set; }
}

public sealed class MenuConfig
{
    public List<MenuItemConfig> Menu { get; set; } = [];
}
