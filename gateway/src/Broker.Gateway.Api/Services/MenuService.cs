using Broker.Gateway.Api.Config;

namespace Broker.Gateway.Api.Services;

public sealed class MenuService
{
    private readonly ConfigLoader _config;

    public MenuService(ConfigLoader config)
    {
        _config = config;
    }

    public List<MenuItemConfig> GetMenuForUser(IReadOnlySet<string> userPermissions)
    {
        return FilterMenu(_config.Menu.Menu, userPermissions);
    }

    private static List<MenuItemConfig> FilterMenu(List<MenuItemConfig> items, IReadOnlySet<string> permissions)
    {
        var result = new List<MenuItemConfig>();

        foreach (var item in items)
        {
            // If item has required permissions, check that user has at least one
            if (item.Permissions is { Count: > 0 })
            {
                if (!item.Permissions.Any(permissions.Contains))
                    continue;
            }

            var filtered = new MenuItemConfig
            {
                Id = item.Id,
                Label = item.Label,
                Icon = item.Icon,
                Path = item.Path,
                Permissions = item.Permissions
            };

            // Recursively filter children
            if (item.Children is { Count: > 0 })
            {
                filtered.Children = FilterMenu(item.Children, permissions);
                // If all children were filtered out, skip this parent too
                if (filtered.Children.Count == 0)
                    continue;
            }

            result.Add(filtered);
        }

        return result;
    }
}
