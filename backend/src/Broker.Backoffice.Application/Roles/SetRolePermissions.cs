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

        // Clear and replace
        role.RolePermissions.Clear();
        var permissions = await db.Permissions
            .Where(p => request.PermissionIds.Contains(p.Id))
            .ToListAsync(ct);

        foreach (var perm in permissions)
            role.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(), RoleId = role.Id, PermissionId = perm.Id,
                CreatedAt = clock.UtcNow, CreatedBy = currentUser.UserName
            });

        role.UpdatedAt = clock.UtcNow;
        role.UpdatedBy = currentUser.UserName;
        await db.SaveChangesAsync(ct);

        audit.EntityType = "Role";
        audit.EntityId = role.Id.ToString();
        audit.BeforeJson = before;
        audit.AfterJson = JsonSerializer.Serialize(permissions.Select(p => p.Code));

        return new RoleDto(role.Id, role.Name, role.Description, role.IsSystem,
            role.RolePermissions.Select(rp => rp.Permission.Code).ToList(),
            role.CreatedAt, role.RowVersion);
    }
}
