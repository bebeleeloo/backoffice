using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Roles;

public sealed record GetRolesQuery : PagedQuery, IRequest<PagedResult<RoleDto>>
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool? IsSystem { get; init; }
    public string? Permission { get; init; }
}

public sealed class GetRolesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetRolesQuery, PagedResult<RoleDto>>
{
    public async Task<PagedResult<RoleDto>> Handle(GetRolesQuery request, CancellationToken ct)
    {
        var query = db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Q))
            query = query.Where(r =>
                r.Name.Contains(request.Q) ||
                (r.Description != null && r.Description.Contains(request.Q)));

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(r => r.Name.Contains(request.Name));

        if (!string.IsNullOrWhiteSpace(request.Description))
            query = query.Where(r => r.Description != null && r.Description.Contains(request.Description));

        if (request.IsSystem.HasValue)
            query = query.Where(r => r.IsSystem == request.IsSystem.Value);

        if (!string.IsNullOrWhiteSpace(request.Permission))
            query = query.Where(r => r.RolePermissions.Any(rp => rp.Permission.Code.Contains(request.Permission)));

        var projected = query.SortBy(request.Sort ?? "Name")
            .Select(r => new RoleDto(r.Id, r.Name, r.Description, r.IsSystem,
                r.RolePermissions.Select(rp => rp.Permission.Code).ToList(),
                r.CreatedAt, r.RowVersion));

        return await projected.ToPagedResultAsync(request.Page, request.PageSize, ct);
    }
}
