using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Audit;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Broker.Backoffice.Api.Filters;

public sealed class AuditActionFilter(
    IAppDbContext db,
    IAuditContext auditContext,
    ICorrelationIdAccessor correlationId,
    IDateTimeProvider clock,
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

        try
        {
            var userId = GetUserId(context.HttpContext);
            var userName = context.HttpContext.User.Identity?.Name;
            var statusCode = context.HttpContext.Response.StatusCode;
            var route = context.ActionDescriptor.DisplayName ?? context.HttpContext.Request.Path;

            var entry = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = userName,
                Action = $"{context.RouteData.Values["controller"]}.{context.RouteData.Values["action"]}",
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
                CreatedAt = clock.UtcNow
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
