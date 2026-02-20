using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Roles;

public sealed record UpdateRoleCommand(
    Guid Id, string Name, string? Description, byte[] RowVersion) : IRequest<RoleDto>;

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.RowVersion).NotEmpty();
    }
}

public sealed class UpdateRoleCommandHandler(
    IAppDbContext db, IDateTimeProvider clock, ICurrentUser currentUser, IAuditContext audit)
    : IRequestHandler<UpdateRoleCommand, RoleDto>
{
    public async Task<RoleDto> Handle(UpdateRoleCommand request, CancellationToken ct)
    {
        var role = await db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Role {request.Id} not found");

        if (role.IsSystem)
            throw new InvalidOperationException("Cannot modify a system role");

        var before = JsonSerializer.Serialize(new { role.Id, role.Name, role.Description });
        db.Roles.Entry(role).Property(r => r.RowVersion).OriginalValue = request.RowVersion;

        role.Name = request.Name;
        role.Description = request.Description;
        role.UpdatedAt = clock.UtcNow;
        role.UpdatedBy = currentUser.UserName;
        await db.SaveChangesAsync(ct);

        audit.EntityType = "Role";
        audit.EntityId = role.Id.ToString();
        audit.BeforeJson = before;
        audit.AfterJson = JsonSerializer.Serialize(new { role.Id, role.Name, role.Description });

        return new RoleDto(role.Id, role.Name, role.Description, role.IsSystem,
            role.RolePermissions.Select(rp => rp.Permission.Code).ToList(),
            role.CreatedAt, role.RowVersion);
    }
}
