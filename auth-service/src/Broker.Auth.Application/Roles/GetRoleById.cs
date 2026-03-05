using Broker.Auth.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Auth.Application.Roles;

public sealed record GetRoleByIdQuery(Guid Id) : IRequest<RoleDto>;

public sealed class GetRoleByIdQueryHandler(IAuthDbContext db) : IRequestHandler<GetRoleByIdQuery, RoleDto>
{
    public async Task<RoleDto> Handle(GetRoleByIdQuery request, CancellationToken ct)
    {
        var role = await db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Role {request.Id} not found");

        return new RoleDto(role.Id, role.Name, role.Description, role.IsSystem,
            role.RolePermissions.Select(rp => rp.Permission.Code).ToList(),
            role.CreatedAt, role.RowVersion);
    }
}
