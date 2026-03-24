using System.Text.Json;
using Broker.Auth.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Auth.Application.Users;

public sealed record DeleteUserCommand(Guid Id) : IRequest;

public sealed class DeleteUserCommandHandler(
    IAuthDbContext db,
    IAuditContext audit,
    IDateTimeProvider clock) : IRequestHandler<DeleteUserCommand>
{
    public async Task Handle(DeleteUserCommand request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"User {request.Id} not found");

        audit.EntityType = "User";
        audit.EntityId = user.Id.ToString();
        audit.BeforeJson = JsonSerializer.Serialize(new { user.Id, user.Username, user.Email });

        // Revoke all active refresh tokens before deleting
        var activeTokens = await db.UserRefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in activeTokens) t.RevokedAt = clock.UtcNow;

        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
    }
}
