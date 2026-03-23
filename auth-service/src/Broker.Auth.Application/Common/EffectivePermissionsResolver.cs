using Broker.Auth.Domain.Identity;

namespace Broker.Auth.Application.Common;

public static class EffectivePermissionsResolver
{
    public static List<string> GetEffectivePermissions(User user)
    {
        var rolePerms = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .ToHashSet();

        foreach (var ov in user.PermissionOverrides)
        {
            if (ov.IsAllowed) rolePerms.Add(ov.Permission.Code);
            else rolePerms.Remove(ov.Permission.Code);
        }

        return rolePerms.ToList();
    }
}
