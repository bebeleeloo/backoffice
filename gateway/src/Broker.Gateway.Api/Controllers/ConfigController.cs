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
    ConfigLoader configLoader) : ControllerBase
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
        configLoader.SaveMenu(config);
        return Ok(new { message = "Menu config saved" });
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
        configLoader.SaveEntities(config);
        return Ok(new { message = "Entities config saved" });
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
        configLoader.SaveUpstreams(config);
        return Ok(new { message = "Upstreams config saved" });
    }

    [HttpPost("reload")]
    [Authorize(Policy = "settings.manage")]
    public IActionResult Reload()
    {
        configLoader.Load();
        return Ok(new { message = "Config reloaded" });
    }

    private string GetUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value
            ?? User.FindFirst("role")?.Value
            ?? "Viewer";
    }
}
