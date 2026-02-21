using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Domain.Accounts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Accounts;

public sealed record GetAccountsQuery : PagedQuery, IRequest<PagedResult<AccountListItemDto>>
{
    public string? Number { get; init; }
    public List<AccountStatus>? Status { get; init; }
    public List<AccountType>? AccountType { get; init; }
    public List<MarginType>? MarginType { get; init; }
    public List<Tariff>? Tariff { get; init; }
    public string? ClearerName { get; init; }
    public string? TradePlatformName { get; init; }
    public string? ExternalId { get; init; }
}

public sealed class GetAccountsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetAccountsQuery, PagedResult<AccountListItemDto>>
{
    public async Task<PagedResult<AccountListItemDto>> Handle(GetAccountsQuery request, CancellationToken ct)
    {
        var query = db.Accounts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Number))
            query = query.Where(a => EF.Functions.Like(a.Number, $"%{request.Number}%"));

        if (!string.IsNullOrWhiteSpace(request.ExternalId))
            query = query.Where(a => a.ExternalId != null && EF.Functions.Like(a.ExternalId, $"%{request.ExternalId}%"));

        if (!string.IsNullOrWhiteSpace(request.ClearerName))
            query = query.Where(a => a.Clearer != null && EF.Functions.Like(a.Clearer.Name, $"%{request.ClearerName}%"));

        if (!string.IsNullOrWhiteSpace(request.TradePlatformName))
            query = query.Where(a => a.TradePlatform != null && EF.Functions.Like(a.TradePlatform.Name, $"%{request.TradePlatformName}%"));

        if (!string.IsNullOrWhiteSpace(request.Q))
            query = query.Where(a =>
                a.Number.Contains(request.Q) ||
                (a.ExternalId != null && a.ExternalId.Contains(request.Q)));

        if (request.Status is { Count: > 0 })
            query = query.Where(a => request.Status.Contains(a.Status));
        if (request.AccountType is { Count: > 0 })
            query = query.Where(a => request.AccountType.Contains(a.AccountType));
        if (request.MarginType is { Count: > 0 })
            query = query.Where(a => request.MarginType.Contains(a.MarginType));
        if (request.Tariff is { Count: > 0 })
            query = query.Where(a => request.Tariff.Contains(a.Tariff));

        var projected = query.SortBy(request.Sort ?? "-CreatedAt")
            .Select(a => new AccountListItemDto(
                a.Id,
                a.Number,
                a.Clearer != null ? a.Clearer.Name : null,
                a.TradePlatform != null ? a.TradePlatform.Name : null,
                a.Status,
                a.AccountType,
                a.MarginType,
                a.OptionLevel,
                a.Tariff,
                a.DeliveryType,
                a.OpenedAt,
                a.ClosedAt,
                a.ExternalId,
                a.CreatedAt,
                a.RowVersion,
                a.Holders.Count));

        return await projected.ToPagedResultAsync(request.Page, request.PageSize, ct);
    }
}
