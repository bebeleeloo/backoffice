using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Clients;

public sealed record DeleteClientCommand(Guid Id) : IRequest;

public sealed class DeleteClientCommandHandler(
    IAppDbContext db,
    IAuditContext audit) : IRequestHandler<DeleteClientCommand>
{
    public async Task Handle(DeleteClientCommand request, CancellationToken ct)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Client {request.Id} not found");

        var hasAccounts = await db.AccountHolders.AnyAsync(h => h.ClientId == request.Id, ct);
        if (hasAccounts)
            throw new InvalidOperationException("Cannot delete client because it is linked to one or more accounts. Remove the client from all accounts first.");

        audit.EntityType = "Client";
        audit.EntityId = client.Id.ToString();
        audit.BeforeJson = JsonSerializer.Serialize(new { client.Id, client.Email, client.ClientType });

        db.Clients.Remove(client);
        await db.SaveChangesAsync(ct);
    }
}
