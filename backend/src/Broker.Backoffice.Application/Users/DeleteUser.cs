using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Users;

public sealed record DeleteUserCommand(Guid Id) : IRequest;

public sealed class DeleteUserCommandHandler(
    IAppDbContext db,
    IAuditContext audit) : IRequestHandler<DeleteUserCommand>
{
    public async Task Handle(DeleteUserCommand request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"User {request.Id} not found");

        audit.EntityType = "User";
        audit.EntityId = user.Id.ToString();
        audit.BeforeJson = JsonSerializer.Serialize(new { user.Id, user.Username, user.Email });

        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
    }
}
