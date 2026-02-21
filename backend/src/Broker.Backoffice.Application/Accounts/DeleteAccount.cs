using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Accounts;

public sealed record DeleteAccountCommand(Guid Id) : IRequest;

public sealed class DeleteAccountCommandHandler(
    IAppDbContext db,
    IAuditContext audit) : IRequestHandler<DeleteAccountCommand>
{
    public async Task Handle(DeleteAccountCommand request, CancellationToken ct)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Account {request.Id} not found");

        audit.EntityType = "Account";
        audit.EntityId = account.Id.ToString();
        audit.BeforeJson = JsonSerializer.Serialize(new { account.Id, account.Number, account.Status });

        db.Accounts.Remove(account);
        await db.SaveChangesAsync(ct);
    }
}
