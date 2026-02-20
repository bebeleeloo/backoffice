using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Auth;

public sealed record GetMeQuery(Guid UserId) : IRequest<UserProfileResponse>;

public sealed class GetMeQueryHandler(IAppDbContext db) : IRequestHandler<GetMeQuery, UserProfileResponse>
{
    public async Task<UserProfileResponse> Handle(GetMeQuery request, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Include(u => u.PermissionOverrides).ThenInclude(po => po.Permission)
            .Include(u => u.DataScopes)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new KeyNotFoundException("User not found");

        var permissions = LoginCommandHandler.GetEffectivePermissions(user);

        return new UserProfileResponse(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            permissions,
            user.DataScopes.Select(ds => new DataScopeDto(ds.ScopeType, ds.ScopeValue)).ToList());
    }
}
