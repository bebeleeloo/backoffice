using Broker.Auth.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Auth.Application.Permissions;

public sealed record GetPermissionsQuery : IRequest<IReadOnlyList<PermissionDto>>;

public sealed class GetPermissionsQueryHandler(IAuthDbContext db)
    : IRequestHandler<GetPermissionsQuery, IReadOnlyList<PermissionDto>>
{
    public async Task<IReadOnlyList<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken ct)
    {
        return await db.Permissions
            .OrderBy(p => p.Group).ThenBy(p => p.Code)
            .Select(p => new PermissionDto(p.Id, p.Code, p.Name, p.Description, p.Group))
            .ToListAsync(ct);
    }
}
