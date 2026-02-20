using Broker.Backoffice.Application.Auth;
using Broker.Backoffice.Domain.Identity;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Unit;

public class PermissionsTests
{
    [Fact]
    public void GetEffectivePermissions_ShouldMergeRolePermissions()
    {
        var perm1 = new Permission { Id = Guid.NewGuid(), Code = "users.read" };
        var perm2 = new Permission { Id = Guid.NewGuid(), Code = "users.create" };
        var role = new Role { Id = Guid.NewGuid(), Name = "Editor" };
        role.RolePermissions.Add(new RolePermission { Permission = perm1 });
        role.RolePermissions.Add(new RolePermission { Permission = perm2 });

        var user = new User { Id = Guid.NewGuid(), Username = "test" };
        user.UserRoles.Add(new UserRole { Role = role });

        var result = LoginCommandHandler.GetEffectivePermissions(user);

        result.Should().Contain("users.read").And.Contain("users.create");
    }

    [Fact]
    public void GetEffectivePermissions_OverrideDeny_ShouldRemovePermission()
    {
        var perm = new Permission { Id = Guid.NewGuid(), Code = "users.read" };
        var role = new Role { Id = Guid.NewGuid(), Name = "Editor" };
        role.RolePermissions.Add(new RolePermission { Permission = perm });

        var user = new User { Id = Guid.NewGuid(), Username = "test" };
        user.UserRoles.Add(new UserRole { Role = role });
        user.PermissionOverrides.Add(new UserPermissionOverride { Permission = perm, IsAllowed = false });

        var result = LoginCommandHandler.GetEffectivePermissions(user);

        result.Should().NotContain("users.read");
    }

    [Fact]
    public void GetEffectivePermissions_OverrideAllow_ShouldAddPermission()
    {
        var perm = new Permission { Id = Guid.NewGuid(), Code = "audit.read" };
        var user = new User { Id = Guid.NewGuid(), Username = "test" };
        user.PermissionOverrides.Add(new UserPermissionOverride { Permission = perm, IsAllowed = true });

        var result = LoginCommandHandler.GetEffectivePermissions(user);

        result.Should().Contain("audit.read");
    }
}
