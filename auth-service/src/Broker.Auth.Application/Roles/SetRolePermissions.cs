using System.Text.Json;
using Broker.Auth.Application.Abstractions;
using Broker.Auth.Domain.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Auth.Application.Roles;

public sealed record SetRolePermissionsCommand(Guid RoleId, List<Guid> PermissionIds) : IRequest<RoleDto>;

public sealed class SetRolePermissionsCommandValidator : AbstractValidator<SetRolePermissionsCommand>
{
    public SetRolePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.PermissionIds).NotNull();
    }
}

public sealed class SetRolePermissionsCommandHandler(
    IAuthDbContext db, IDateTimeProvider clock, ICurrentUser currentUser, IAuditContext audit)
    : IRequestHandler<SetRolePermissionsCommand, RoleDto>
{
    public async Task<RoleDto> Handle(SetRolePermissionsCommand request, CancellationToken ct)
    {
        var role = await db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, ct)
            ?? throw new KeyNotFoundException($"Role {request.RoleId} not found");

        if (role.IsSystem)
            throw new InvalidOperationException("Cannot modify permissions on a system role");

        var before = JsonSerializer.Serialize(role.RolePermissions.Select(rp => rp.Permission.Code));

        var currentPermIds = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
        var toRemove = role.RolePermissions.Where(rp => !request.PermissionIds.Contains(rp.PermissionId)).ToList();
        foreach (var rp in toRemove) role.RolePermissions.Remove(rp);

        var newPermIds = request.PermissionIds.Where(id => !currentPermIds.Contains(id)).ToList();
        if (newPermIds.Count > 0)
        {
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
