using Microsoft.AspNetCore.Authorization;

namespace Broker.Backoffice.Infrastructure.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class HasPermissionAttribute(string permission)
    : AuthorizeAttribute(policy: permission)
{
    public string Permission { get; } = permission;
}
