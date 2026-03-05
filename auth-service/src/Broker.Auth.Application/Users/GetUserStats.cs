using Broker.Auth.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Auth.Application.Users;

public sealed record UserStatsDto(int TotalUsers, int ActiveUsers);

public sealed record GetUserStatsQuery : IRequest<UserStatsDto>;

public sealed class GetUserStatsQueryHandler(IAuthDbContext db)
    : IRequestHandler<GetUserStatsQuery, UserStatsDto>
{
    public async Task<UserStatsDto> Handle(GetUserStatsQuery request, CancellationToken ct)
    {
        var total = await db.Users.CountAsync(ct);
        var active = await db.Users.CountAsync(u => u.IsActive, ct);
        return new UserStatsDto(total, active);
    }
}
