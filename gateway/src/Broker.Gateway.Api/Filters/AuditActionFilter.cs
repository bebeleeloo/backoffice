using Broker.Gateway.Api.Domain;
using Broker.Gateway.Api.Persistence;
using Broker.Gateway.Api.Services;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Broker.Gateway.Api.Filters;

public sealed class AuditActionFilter(
    GatewayDbContext db,
    IAuditContext auditContext,
    ICorrelationIdAccessor correlationId,
    ILogger<AuditActionFilter> logger) : IAsyncActionFilter
{
    private static readonly HashSet<string> AuditedMethods = ["POST", "PUT", "PATCH", "DELETE"];

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var method = context.HttpContext.Request.Method;
        if (!AuditedMethods.Contains(method))
        {
            await next();
            return;
        }

        var result = await next();

        // NOTE: Audit log is saved in a separate SaveChangesAsync call intentionally.
        // If audit save fails, the business operation should NOT be rolled back.
        // This is a non-critical trail — errors are logged and swallowed.
        try
        {
            var userId = GetUserId(context.HttpContext);
            var userName = context.HttpContext.User.Identity?.Name;
            var statusCode = context.HttpContext.Response.StatusCode;

            if (string.IsNullOrEmpty(auditContext.EntityType))
                logger.LogWarning("AuditContext.EntityType not set for {Method} {Path}", method, context.HttpContext.Request.Path);

            var controller = context.RouteData.Values["controller"]?.ToString() ?? "unknown";
            var action = context.RouteData.Values["action"]?.ToString() ?? "unknown";

            var entry = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = userName,
                Action = $"{controller}.{action}",
                EntityType = auditContext.EntityType,
                EntityId = auditContext.EntityId,
                BeforeJson = auditContext.BeforeJson,
                AfterJson = auditContext.AfterJson,
                CorrelationId = correlationId.CorrelationId,
                IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.HttpContext.Request.Headers.UserAgent.ToString(),
                Path = context.HttpContext.Request.Path,
                Method = method,
                StatusCode = statusCode,
                IsSuccess = result.Exception is null && statusCode < 400,
                CreatedAt = DateTime.UtcNow
            };

            db.AuditLogs.Add(entry);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write audit log");
        }
    }

    private static Guid? GetUserId(HttpContext context)
    {
        var sub = context.User.FindFirst("sub")?.Value
                  ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
