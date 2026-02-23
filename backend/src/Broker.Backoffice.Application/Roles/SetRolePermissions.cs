using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Roles;

public sealed record SetRolePermissionsCommand(Guid RoleId, List<Guid> PermissionIds) : IRequest<RoleDto>;

public sealed class SetRolePermissionsCommandHandler(
    IAppDbContext db, IDateTimeProvider clock, ICurrentUser currentUser, IAuditContext audit)
    : IRequestHandler<SetRolePermissionsCommand, RoleDto>
{
    public async Task<RoleDto> Handle(SetRolePermissionsCommand request, CancellationToken ct)
    {
        var role = await db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, ct)
            ?? throw new KeyNotFoundException($"Role {request.RoleId} not found");

        var before = JsonSerializer.Serialize(role.RolePermissions.Select(rp => rp.Permission.Code));

        // Sync permissions (diff)
        var currentPermIds = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
        var toRemove = role.RolePermissions.Where(rp => !request.PermissionIds.Contains(rp.PermissionId)).ToList();
        foreach (var rp in toRemove) role.RolePermissions.Remove(rp);

        var newPermIds = request.PermissionIds.Where(id => !currentPermIds.Contains(id)).ToList();
        if (newPermIds.Count > 0)
        {
            // Load permissions into context so EF fixup populates rp.Permission navigation
            await db.Permissions.Where(p => newPermIds.Contains(p.Id)).LoadAsync(ct);
        }

        foreach (var permId in newPermIds)
        {
            var rp = new RolePermission
            {
                RoleId = role.Id, PermissionId = permId,
                CreatedAt = clock.UtcNow, CreatedBy = currentUser.UserName
            };
            role.RolePermissions.Add(rp);
            db.RolePermissions.Add(rp);
        }

        role.UpdatedAt = clock.UtcNow;
        role.UpdatedBy = currentUser.UserName;
        await db.SaveChangesAsync(ct);

        audit.EntityType = "Role";
        audit.EntityId = role.Id.ToString();
        audit.BeforeJson = before;
        audit.AfterJson = JsonSerializer.Serialize(role.RolePermissions.Select(rp => rp.Permission.Code));

        return new RoleDto(role.Id, role.Name, role.Description, role.IsSystem,
            role.RolePermissions.Select(rp => rp.Permission.Code).ToList(),
            role.CreatedAt, role.RowVersion);
    }
}
