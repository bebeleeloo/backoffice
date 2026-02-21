using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Accounts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Accounts;

public sealed record GetAccountByIdQuery(Guid Id) : IRequest<AccountDto>;

public sealed class GetAccountByIdQueryHandler(IAppDbContext db) : IRequestHandler<GetAccountByIdQuery, AccountDto>
{
    public async Task<AccountDto> Handle(GetAccountByIdQuery request, CancellationToken ct)
    {
        var a = await db.Accounts
            .Include(x => x.Clearer)
            .Include(x => x.TradePlatform)
            .Include(x => x.Holders).ThenInclude(h => h.Client)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Account {request.Id} not found");

        return ToDto(a);
    }

    internal static AccountDto ToDto(Account a) => new(
        a.Id, a.Number,
        a.ClearerId, a.Clearer?.Name,
        a.TradePlatformId, a.TradePlatform?.Name,
        a.Status, a.AccountType, a.MarginType, a.OptionLevel, a.Tariff,
        a.DeliveryType,
        a.OpenedAt, a.ClosedAt,
        a.Comment, a.ExternalId,
        a.CreatedAt, a.RowVersion,
        a.Holders.Select(h => new AccountHolderDto(
            h.ClientId,
            h.Client is not null
                ? (h.Client.ClientType == Domain.Clients.ClientType.Corporate
                    ? h.Client.CompanyName ?? ""
                    : $"{h.Client.FirstName} {h.Client.LastName}".Trim())
                : "",
            h.Role,
            h.IsPrimary,
            h.AddedAt
        )).ToList());
}
