using Broker.Backoffice.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Broker.Backoffice.Infrastructure.Services;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string? UserId =>
        httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

    public string? UserName =>
        httpContextAccessor.HttpContext?.User?.Identity?.Name;

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
