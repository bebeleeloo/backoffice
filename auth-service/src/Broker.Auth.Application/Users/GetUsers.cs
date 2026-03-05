using Broker.Auth.Application.Abstractions;
using Broker.Auth.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Auth.Application.Users;

public sealed record GetUsersQuery : PagedQuery, IRequest<PagedResult<UserDto>>
{
    public bool? IsActive { get; init; }
    public string? Username { get; init; }
    public string? Email { get; init; }
    public string? FullName { get; init; }
    public string? Role { get; init; }
}

public sealed class GetUsersQueryHandler(IAuthDbContext db)
    : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var query = db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var qPattern = LikeHelper.ContainsPattern(request.Q);
            query = query.Where(u =>
                EF.Functions.Like(u.Username, qPattern) ||
                EF.Functions.Like(u.Email, qPattern) ||
                (u.FullName != null && EF.Functions.Like(u.FullName, qPattern)));
        }

        if (request.IsActive.HasValue)
            query = query.Where(u => u.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var pattern = LikeHelper.ContainsPattern(request.Username);
            query = query.Where(u => EF.Functions.Like(u.Username, pattern));
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var pattern = LikeHelper.ContainsPattern(request.Email);
            query = query.Where(u => EF.Functions.Like(u.Email, pattern));
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            var pattern = LikeHelper.ContainsPattern(request.FullName);
            query = query.Where(u => u.FullName != null && EF.Functions.Like(u.FullName, pattern));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var pattern = LikeHelper.ContainsPattern(request.Role);
            query = query.Where(u => u.UserRoles.Any(ur => EF.Functions.Like(ur.Role.Name, pattern)));
        }

        var projected = query.SortBy(request.Sort ?? "-CreatedAt")
            .Select(u => new UserDto(
                u.Id, u.Username, u.Email, u.FullName, u.IsActive,
                u.Photo != null,
                u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                u.CreatedAt, u.RowVersion));

        return await projected.ToPagedResultAsync(request.Page, request.PageSize, ct);
    }
}
