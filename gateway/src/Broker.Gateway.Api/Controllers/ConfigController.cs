using System.Security.Claims;
using Broker.Gateway.Api.Config;
using Broker.Gateway.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Broker.Gateway.Api.Controllers;

[ApiController]
[Route("api/v1/config")]
[Authorize]
public sealed class ConfigController(
    MenuService menuService,
    EntityConfigService entityConfigService,
    ConfigLoader configLoader,
    ILogger<ConfigController> logger) : ControllerBase
{
    [HttpGet("menu")]
    public IActionResult GetMenu()
    {
        var permissions = User.FindAll("permission")
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var menu = menuService.GetMenuForUser(permissions);
        return Ok(menu);
    }

    [HttpGet("menu/raw")]
    [Authorize(Policy = "settings.manage")]
    public IActionResult GetMenuRaw()
    {
        return Ok(configLoader.Menu.Menu);
    }

    [HttpPut("menu")]
    [Authorize(Policy = "settings.manage")]
    public IActionResult SaveMenu([FromBody] MenuConfig config)
    {
        if (config == null)
            return BadRequest(new { type = "validation", title = "Validation error", detail = "Config body is required." });

        if (config.Menu == null || config.Menu.Count == 0)
            return BadRequest(new { type = "validation", title = "Validation error", detail = "Menu must contain at least one item." });

        var validationError = ValidateMenuItems(config.Menu);
        if (validationError != null)
            return BadRequest(new { type = "validation", title = "Validation error", detail = validationError });

        try
        {
            configLoader.SaveMenu(config);
            return Ok(new { message = "Menu config saved" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save menu config");
            return StatusCode(500, new { type = "error", title = "Internal error", detail = "Failed to save menu configuration." });
        }
    }

    [HttpGet("entities")]
    public IActionResult GetEntities()
    {
        var role = GetUserRole();
        var entities = entityConfigService.GetEntitiesForRole(role);
        return Ok(entities);
    }

    [HttpGet("entities/raw")]
    [Authorize(Policy = "settings.manage")]
    public IActionResult GetEntitiesRaw()
    {
        return Ok(configLoader.Entities.Entities);
    }

    [HttpGet("entities/{name}")]
    public IActionResult GetEntity(string name)
    {
        var role = GetUserRole();
        var entity = entityConfigService.GetEntityForRole(name, role);
        if (entity == null) return NotFound();
        return Ok(entity);
    }

    [HttpPut("entities")]
    [Authorize(Policy = "settings.manage")]
    public IActionResult SaveEntities([FromBody] EntitiesConfig config)
    {
        if (config == null)
            return BadRequest(new { type = "validation", title = "Validation error", detail = "Config body is required." });

        if (config.Entities == null)
            return BadRequest(new { type = "validation", title = "Validation error", detail = "Entities list is required." });

        try
        {
            configLoader.SaveEntities(config);
            return Ok(new { message = "Entities config saved" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save entities config");
            return StatusCode(500, new { type = "error", title = "Internal error", detail = "Failed to save entities configuration." });
        }
    }

    [HttpGet("upstreams")]
    [Authorize(Policy = "settings.manage")]
    public IActionResult GetUpstreams()
    {
        return Ok(configLoader.Upstreams.Upstreams);
    }

    [HttpPut("upstreams")]
    [Authorize(Policy = "settings.manage")]
    public IActionResult SaveUpstreams([FromBody] UpstreamsConfig config)
    {
        if (config == null)
            return BadRequest(new { type = "validation", title = "Validation error", detail = "Config body is required." });

        if (config.Upstreams == null)
            return BadRequest(new { type = "validation", title = "Validation error", detail = "Upstreams list is required." });

        foreach (var (name, upstream) in config.Upstreams)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { type = "validation", title = "Validation error", detail = "All upstreams must have a non-empty name." });

            if (string.IsNullOrWhiteSpace(upstream.Address))
                return BadRequest(new { type = "validation", title = "Validation error", detail = $"Upstream '{name}' must have a non-empty Address." });

            if (!Uri.TryCreate(upstream.Address, UriKind.Absolute, out _))
                return BadRequest(new { type = "validation", title = "Validation error", detail = $"Upstream '{name}' has an invalid Address URI: '{upstream.Address}'." });

            if (upstream.Routes == null || upstream.Routes.Count == 0)
                return BadRequest(new { type = "validation", title = "Validation error", detail = $"Upstream '{name}' must have at least one route." });

            foreach (var route in upstream.Routes)
            {
                if (!route.StartsWith('/'))
                    return BadRequest(new { type = "validation", title = "Validation error", detail = $"Upstream '{name}' route '{route}' must start with '/'." });
            }
        }

        try
        {
            configLoader.SaveUpstreams(config);
            return Ok(new { message = "Upstreams config saved" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save upstreams config");
            return StatusCode(500, new { type = "error", title = "Internal error", detail = "Failed to save upstreams configuration." });
        }
    }

    [HttpPost("reload")]
    [Authorize(Policy = "settings.manage")]
    public IActionResult Reload()
    {
        configLoader.Load();
        return Ok(new { message = "Config reloaded" });
    }

    /// <summary>
    /// Extracts the user's role from JWT claims.
    /// Checks both <see cref="ClaimTypes.Role"/> (standard) and "role" (custom) claim types.
    /// Falls back to "Viewer" role if no role claim is found, logging a warning with the user ID.
    /// </summary>
    private string GetUserRole()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value
            ?? User.FindFirst("role")?.Value;

        if (role == null)
        {
            logger.LogWarning(
                "No role claim found for user {UserId}, falling back to default role 'Viewer'",
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown");
            return "Viewer";
        }

        return role;
    }

    private static string? ValidateMenuItems(IReadOnlyList<MenuItemConfig> items)
    {
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
                return "All menu items must have a non-empty Id.";
            if (string.IsNullOrWhiteSpace(item.Label))
                return $"Menu item '{item.Id}' must have a non-empty Label.";
            if (item.Children is { Count: > 0 })
            {
                var childError = ValidateMenuItems(item.Children);
                if (childError != null) return childError;
            }
        }
        return null;
    }
}
