using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Roles;

public sealed record GetRoleByIdQuery(Guid Id) : IRequest<RoleDto>;

public sealed class GetRoleByIdQueryHandler(IAppDbContext db) : IRequestHandler<GetRoleByIdQuery, RoleDto>
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
