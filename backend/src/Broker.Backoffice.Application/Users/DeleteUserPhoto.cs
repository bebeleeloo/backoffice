using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Users;

public sealed record DeleteUserPhotoCommand(Guid UserId) : IRequest;

internal sealed class DeleteUserPhotoCommandHandler(IAppDbContext db, IAuditContext audit)
    : IRequestHandler<DeleteUserPhotoCommand>
{
    public async Task Handle(DeleteUserPhotoCommand request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found");

        audit.EntityType = "User";
        audit.EntityId = user.Id.ToString();

        user.Photo = null;
        user.PhotoContentType = null;
        await db.SaveChangesAsync(ct);
    }
}
