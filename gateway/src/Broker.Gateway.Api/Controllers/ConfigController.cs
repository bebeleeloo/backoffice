using System.Security.Claims;
using System.Text.Json;
using Broker.Gateway.Api.Config;
using Broker.Gateway.Api.Domain;
using Broker.Gateway.Api.Filters;
using Broker.Gateway.Api.Persistence;
using Broker.Gateway.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Broker.Gateway.Api.Controllers;

[ApiController]
[Route("api/v1/config")]
[Authorize]
public sealed class ConfigController(
    MenuService menuService,
    EntityConfigService entityConfigService,
    ConfigLoader configLoader,
    ConfigDiffService diffService,
    GatewayDbContext db,
    IAuditContext auditContext,
    ILogger<ConfigController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

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
    [ServiceFilter(typeof(AuditActionFilter))]
    public IActionResult SaveMenu([FromBody] MenuConfig config)
    {
        if (config == null)
            return BadRequest(new { type = "validation", title = "Validation error", detail = "Config body is required." });

        if (config.Menu == null || config.Menu.Count == 0)
            return BadRequest(new { type = "validation", title = "Validation error", detail = "Menu must contain at least one item." });

        var validationError = ValidateMenuItems(config.Menu);
        if (validationError != null)
            return BadRequest(new { type = "validation", title = "Validation error", detail = validationError });

        auditContext.EntityType = "MenuConfig";
        auditContext.EntityId = "config";
        auditContext.BeforeJson = JsonSerializer.Serialize(configLoader.Menu.Menu, JsonOptions);

        configLoader.SaveMenu(config);

        auditContext.AfterJson = JsonSerializer.Serialize(config.Menu, JsonOptions);
        return Ok(new { message = "Menu config saved" });
    }

    [HttpGet("entities")]
    public IActionResult GetEntities()
    {
        var roles = GetUserRoles();
        var entities = entityConfigService.GetEntitiesForRole(roles);
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
        var roles = GetUserRoles();
        var entity = entityConfigService.GetEntityForRole(name, roles);
        if (entity == null) return NotFound();
        return Ok(entity);
    }

    [HttpPut("entities")]
    [Authorize(Policy = "settings.manage")]
    [ServiceFilter(typeof(AuditActionFilter))]
    public IActionResult SaveEntities([FromBody] EntitiesConfig config)
    {
        if (config == null)
            return BadRequest(new { type = "validation", title = "Validation error", detail = "Config body is required." });

        if (config.Entities == null)
            return BadRequest(new { type = "validation", title = "Validation error", detail = "Entities list is required." });

        auditContext.EntityType = "EntitiesConfig";
        auditContext.EntityId = "config";
        auditContext.BeforeJson = JsonSerializer.Serialize(configLoader.Entities.Entities, JsonOptions);

        configLoader.SaveEntities(config);

        auditContext.AfterJson = JsonSerializer.Serialize(config.Entities, JsonOptions);
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
    [ServiceFilter(typeof(AuditActionFilter))]
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

        auditContext.EntityType = "UpstreamsConfig";
        auditContext.EntityId = "config";
        auditContext.BeforeJson = JsonSerializer.Serialize(configLoader.Upstreams.Upstreams, JsonOptions);

        configLoader.SaveUpstreams(config);

        auditContext.AfterJson = JsonSerializer.Serialize(config.Upstreams, JsonOptions);
        return Ok(new { message = "Upstreams config saved" });
    }

    [HttpPost("reload")]
    [Authorize(Policy = "settings.manage")]
    [ServiceFilter(typeof(AuditActionFilter))]
    public IActionResult Reload()
    {
        auditContext.EntityType = "Config";
        auditContext.EntityId = "reload";

        configLoader.Load();
        return Ok(new { message = "Config reloaded" });
    }

    [HttpGet("entity-changes")]
    [Authorize(Policy = "settings.manage")]
    public async Task<IActionResult> GetEntityChanges(
        [FromQuery] string entityType,
        [FromQuery] string entityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 10000);

        var query = db.AuditLogs
            .Where(a => a.EntityType == entityType && a.IsSuccess)
            .OrderByDescending(a => a.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var logs = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = logs.Select(a => new OperationDto(
            a.Id.ToString(),
            a.CreatedAt,
            a.UserId,
            a.UserName,
            a.EntityType switch
            {
                "MenuConfig" => "Menu Configuration",
                "EntitiesConfig" => "Entity Fields",
                "UpstreamsConfig" => "Upstreams",
                _ => a.EntityType ?? "Configuration",
            },
            diffService.DetermineChangeType(a.BeforeJson, a.AfterJson),
            diffService.ComputeDiff(a.EntityType, a.BeforeJson, a.AfterJson)
        )).ToList();

        return Ok(new
        {
            items,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
        });
    }

    [HttpGet("entity-changes/all")]
    [Authorize(Policy = "settings.manage")]
    public async Task<IActionResult> GetAllEntityChanges(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? sort = null,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? changeType = null,
        [FromQuery(Name = "userName")] string[]? userName = null,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 10000);

        IQueryable<AuditLog> query = db.AuditLogs.Where(a => a.IsSuccess);

        if (DateTime.TryParse(from, out var fromDate))
            query = query.Where(a => a.CreatedAt >= fromDate);

        if (DateTime.TryParse(to, out var toDate))
            query = query.Where(a => a.CreatedAt < toDate.AddDays(1));

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (userName is { Length: > 0 })
            query = query.Where(a => a.UserName != null && userName.Contains(a.UserName));

        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{q.Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(a =>
                EF.Functions.ILike(a.UserName!, pattern) ||
                EF.Functions.ILike(a.EntityType!, pattern));
        }

        var totalCount = await query.CountAsync(ct);

        query = ApplySort(query, sort);

        var logs = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = logs
            .Select(a =>
            {
                var ct2 = diffService.DetermineChangeType(a.BeforeJson, a.AfterJson);
                return new GlobalOperationDto(
                    a.Id.ToString(),
                    a.CreatedAt,
                    a.UserId,
                    a.UserName,
                    a.EntityType ?? "Config",
                    a.EntityId ?? "config",
                    a.EntityType switch
                    {
                        "MenuConfig" => "Menu Configuration",
                        "EntitiesConfig" => "Entity Fields",
                        "UpstreamsConfig" => "Upstreams",
                        _ => "Configuration",
                    },
                    ct2,
                    diffService.ComputeDiff(a.EntityType, a.BeforeJson, a.AfterJson));
            })
            .Where(op => string.IsNullOrWhiteSpace(changeType) || op.ChangeType == changeType)
            .ToList();

        // Adjust totalCount if changeType filter applied in-memory
        if (!string.IsNullOrWhiteSpace(changeType))
        {
            totalCount = items.Count;
        }

        return Ok(new
        {
            items,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
        });
    }

    private static IQueryable<AuditLog> ApplySort(IQueryable<AuditLog> query, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return query.OrderByDescending(a => a.CreatedAt);

        var parts = sort.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var field = parts[0];
        var desc = parts.Length > 1 && string.Equals(parts[1], "desc", StringComparison.OrdinalIgnoreCase);

        return field.ToLowerInvariant() switch
        {
            "timestamp" => desc ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt),
            "username" => desc ? query.OrderByDescending(a => a.UserName) : query.OrderBy(a => a.UserName),
            "entitytype" => desc ? query.OrderByDescending(a => a.EntityType) : query.OrderBy(a => a.EntityType),
            _ => query.OrderByDescending(a => a.CreatedAt),
        };
    }

    private IReadOnlyList<string> GetUserRoles()
    {
        var roles = User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        if (roles.Count == 0)
        {
            logger.LogWarning(
                "No role claims found for user {UserId}, falling back to default role 'Viewer'",
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown");
            return ["Viewer"];
        }

        return roles;
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
