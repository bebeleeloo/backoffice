using Broker.Auth.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Auth.Application.Users;

public sealed record GetUserByIdQuery(Guid Id) : IRequest<UserDto>;

public sealed class GetUserByIdQueryHandler(IAuthDbContext db) : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"User {request.Id} not found");

        return new UserDto(user.Id, user.Username, user.Email, user.FullName, user.IsActive,
            user.Photo != null,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(), user.CreatedAt, user.RowVersion);
    }
}
