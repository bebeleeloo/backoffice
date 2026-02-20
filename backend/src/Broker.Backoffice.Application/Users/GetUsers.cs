using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Users;

public sealed record GetUsersQuery : PagedQuery, IRequest<PagedResult<UserDto>>
{
    public bool? IsActive { get; init; }
    public string? Username { get; init; }
    public string? Email { get; init; }
    public string? FullName { get; init; }
    public string? Role { get; init; }
}

public sealed class GetUsersQueryHandler(IAppDbContext db)
    : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var query = db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Q))
            query = query.Where(u =>
                u.Username.Contains(request.Q) ||
                u.Email.Contains(request.Q) ||
                (u.FullName != null && u.FullName.Contains(request.Q)));

        if (request.IsActive.HasValue)
            query = query.Where(u => u.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.Username))
            query = query.Where(u => u.Username.Contains(request.Username));

        if (!string.IsNullOrWhiteSpace(request.Email))
            query = query.Where(u => u.Email.Contains(request.Email));

        if (!string.IsNullOrWhiteSpace(request.FullName))
            query = query.Where(u => u.FullName != null && u.FullName.Contains(request.FullName));

        if (!string.IsNullOrWhiteSpace(request.Role))
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name.Contains(request.Role)));

        var projected = query.SortBy(request.Sort ?? "-CreatedAt")
            .Select(u => new UserDto(
                u.Id, u.Username, u.Email, u.FullName, u.IsActive,
                u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                u.CreatedAt, u.RowVersion));

        return await projected.ToPagedResultAsync(request.Page, request.PageSize, ct);
    }
}
