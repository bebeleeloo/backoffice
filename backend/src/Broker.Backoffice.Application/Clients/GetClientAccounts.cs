using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Application.Accounts;
using Broker.Backoffice.Domain.Accounts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Clients;

public sealed record ClientAccountDto(
    Guid AccountId,
    string AccountNumber,
    AccountStatus AccountStatus,
    HolderRole Role,
    bool IsPrimary,
    DateTime AddedAt);

public sealed record GetClientAccountsQuery(Guid ClientId) : IRequest<IReadOnlyList<ClientAccountDto>>;

public sealed class GetClientAccountsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetClientAccountsQuery, IReadOnlyList<ClientAccountDto>>
{
    public async Task<IReadOnlyList<ClientAccountDto>> Handle(GetClientAccountsQuery request, CancellationToken ct)
    {
        var exists = await db.Clients.AnyAsync(c => c.Id == request.ClientId, ct);
        if (!exists)
            throw new KeyNotFoundException($"Client {request.ClientId} not found");

        return await db.AccountHolders
            .Where(h => h.ClientId == request.ClientId)
            .Include(h => h.Account)
            .Select(h => new ClientAccountDto(
                h.AccountId,
                h.Account!.Number,
                h.Account.Status,
                h.Role,
                h.IsPrimary,
                h.AddedAt))
            .ToListAsync(ct);
    }
}
