using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Users;

public sealed record UserPhotoResult(byte[] Photo, string ContentType);

public sealed record GetUserPhotoQuery(Guid UserId) : IRequest<UserPhotoResult>;

internal sealed class GetUserPhotoQueryHandler(IAppDbContext db)
    : IRequestHandler<GetUserPhotoQuery, UserPhotoResult>
{
    public async Task<UserPhotoResult> Handle(GetUserPhotoQuery request, CancellationToken ct)
    {
        var user = await db.Users
            .Where(u => u.Id == request.UserId)
            .Select(u => new { u.Photo, u.PhotoContentType })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found");

        if (user.Photo is null || user.PhotoContentType is null)
            throw new KeyNotFoundException("User has no photo");

        return new UserPhotoResult(user.Photo, user.PhotoContentType);
    }
}
