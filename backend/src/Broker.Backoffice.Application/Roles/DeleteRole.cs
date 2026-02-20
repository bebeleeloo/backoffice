using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Roles;

public sealed record DeleteRoleCommand(Guid Id) : IRequest;

public sealed class DeleteRoleCommandHandler(
    IAppDbContext db, IAuditContext audit) : IRequestHandler<DeleteRoleCommand>
{
    public async Task Handle(DeleteRoleCommand request, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Role {request.Id} not found");

        if (role.IsSystem)
            throw new InvalidOperationException("Cannot delete a system role");

        audit.EntityType = "Role";
        audit.EntityId = role.Id.ToString();
        audit.BeforeJson = JsonSerializer.Serialize(new { role.Id, role.Name });

        db.Roles.Remove(role);
        await db.SaveChangesAsync(ct);
    }
}
