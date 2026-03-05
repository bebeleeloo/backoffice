using Broker.Auth.Application.Abstractions;
using Broker.Auth.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Auth.Application.Roles;

public sealed record GetRolesQuery : PagedQuery, IRequest<PagedResult<RoleDto>>
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool? IsSystem { get; init; }
    public string? Permission { get; init; }
}

public sealed class GetRolesQueryHandler(IAuthDbContext db)
    : IRequestHandler<GetRolesQuery, PagedResult<RoleDto>>
{
    public async Task<PagedResult<RoleDto>> Handle(GetRolesQuery request, CancellationToken ct)
    {
        var query = db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var qPattern = LikeHelper.ContainsPattern(request.Q);
            query = query.Where(r =>
                EF.Functions.Like(r.Name, qPattern) ||
                (r.Description != null && EF.Functions.Like(r.Description, qPattern)));
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var pattern = LikeHelper.ContainsPattern(request.Name);
            query = query.Where(r => EF.Functions.Like(r.Name, pattern));
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            var pattern = LikeHelper.ContainsPattern(request.Description);
            query = query.Where(r => r.Description != null && EF.Functions.Like(r.Description, pattern));
        }

        if (request.IsSystem.HasValue)
            query = query.Where(r => r.IsSystem == request.IsSystem.Value);

        if (!string.IsNullOrWhiteSpace(request.Permission))
        {
            var pattern = LikeHelper.ContainsPattern(request.Permission);
            query = query.Where(r => r.RolePermissions.Any(rp => EF.Functions.Like(rp.Permission.Code, pattern)));
        }

        var projected = query.SortBy(request.Sort ?? "Name")
            .Select(r => new RoleDto(r.Id, r.Name, r.Description, r.IsSystem,
                r.RolePermissions.Select(rp => rp.Permission.Code).ToList(),
                r.CreatedAt, r.RowVersion));

        return await projected.ToPagedResultAsync(request.Page, request.PageSize, ct);
    }
}
