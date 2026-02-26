using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Orders.NonTradeOrders;

public sealed record GetNonTradeOrdersQuery : PagedQuery, IRequest<PagedResult<NonTradeOrderListItemDto>>
{
    public List<OrderStatus>? Status { get; init; }
    public List<NonTradeOrderType>? NonTradeType { get; init; }
    public List<Guid>? AccountId { get; init; }
    public List<Guid>? InstrumentId { get; init; }
    public string? OrderNumber { get; init; }
    public string? CurrencyCode { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? ExternalId { get; init; }
    public DateTime? OrderDateFrom { get; init; }
    public DateTime? OrderDateTo { get; init; }
    public DateTime? CreatedFrom { get; init; }
    public DateTime? CreatedTo { get; init; }
    public DateTime? ProcessedFrom { get; init; }
    public DateTime? ProcessedTo { get; init; }
    public decimal? AmountMin { get; init; }
    public decimal? AmountMax { get; init; }
}

public sealed class GetNonTradeOrdersQueryHandler(IAppDbContext db)
    : IRequestHandler<GetNonTradeOrdersQuery, PagedResult<NonTradeOrderListItemDto>>
{
    public async Task<PagedResult<NonTradeOrderListItemDto>> Handle(GetNonTradeOrdersQuery request, CancellationToken ct)
    {
        var query = db.NonTradeOrders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.OrderNumber))
            query = query.Where(n => EF.Functions.Like(n.Order!.OrderNumber, $"%{request.OrderNumber}%"));

        if (!string.IsNullOrWhiteSpace(request.ExternalId))
            query = query.Where(n => n.Order!.ExternalId != null && EF.Functions.Like(n.Order.ExternalId, $"%{request.ExternalId}%"));

        if (request.AccountId is { Count: > 0 })
            query = query.Where(n => request.AccountId.Contains(n.Order!.AccountId));

        if (request.InstrumentId is { Count: > 0 })
            query = query.Where(n => n.InstrumentId != null && request.InstrumentId.Contains(n.InstrumentId.Value));

        if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
            query = query.Where(n => EF.Functions.Like(n.Currency!.Code, $"%{request.CurrencyCode}%"));

        if (!string.IsNullOrWhiteSpace(request.ReferenceNumber))
            query = query.Where(n => n.ReferenceNumber != null && EF.Functions.Like(n.ReferenceNumber, $"%{request.ReferenceNumber}%"));

        if (!string.IsNullOrWhiteSpace(request.Q))
            query = query.Where(n =>
                n.Order!.OrderNumber.Contains(request.Q) ||
                n.Order.Account!.Number.Contains(request.Q) ||
                (n.ReferenceNumber != null && n.ReferenceNumber.Contains(request.Q)) ||
                (n.Order.ExternalId != null && n.Order.ExternalId.Contains(request.Q)));

        if (request.Status is { Count: > 0 })
            query = query.Where(n => request.Status.Contains(n.Order!.Status));
        if (request.NonTradeType is { Count: > 0 })
            query = query.Where(n => request.NonTradeType.Contains(n.NonTradeType));

        if (request.OrderDateFrom.HasValue)
            query = query.Where(n => n.Order!.OrderDate >= request.OrderDateFrom.Value);
        if (request.OrderDateTo.HasValue)
            query = query.Where(n => n.Order!.OrderDate < request.OrderDateTo.Value.AddDays(1));

        if (request.CreatedFrom.HasValue)
            query = query.Where(n => n.Order!.CreatedAt >= request.CreatedFrom.Value);
        if (request.CreatedTo.HasValue)
            query = query.Where(n => n.Order!.CreatedAt < request.CreatedTo.Value.AddDays(1));

        if (request.ProcessedFrom.HasValue)
            query = query.Where(n => n.ProcessedAt >= request.ProcessedFrom.Value);
        if (request.ProcessedTo.HasValue)
            query = query.Where(n => n.ProcessedAt < request.ProcessedTo.Value.AddDays(1));

        if (request.AmountMin.HasValue)
            query = query.Where(n => n.Amount >= request.AmountMin.Value);
        if (request.AmountMax.HasValue)
            query = query.Where(n => n.Amount <= request.AmountMax.Value);

        query = ApplySort(query, request.Sort ?? "-CreatedAt");

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NonTradeOrderListItemDto(
                n.OrderId,
                n.Order!.Account!.Number,
                n.Order.OrderNumber,
                n.Order.Status,
                n.Order.OrderDate,
                n.NonTradeType,
                n.Amount,
                n.Currency!.Code,
                n.Instrument != null ? n.Instrument.Symbol : null,
                n.Instrument != null ? n.Instrument.Name : null,
                n.ReferenceNumber,
                n.ProcessedAt,
                n.Order.ExternalId,
                n.Order.CreatedAt,
                n.Order.RowVersion))
            .ToListAsync(ct);

        return new PagedResult<NonTradeOrderListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    private static IQueryable<NonTradeOrder> ApplySort(IQueryable<NonTradeOrder> query, string sort)
    {
        var desc = sort.StartsWith('-');
        var prop = (desc ? sort[1..] : sort).ToLowerInvariant();
        return prop switch
        {
            "ordernumber" => desc ? query.OrderByDescending(n => n.Order!.OrderNumber) : query.OrderBy(n => n.Order!.OrderNumber),
            "orderdate" => desc ? query.OrderByDescending(n => n.Order!.OrderDate) : query.OrderBy(n => n.Order!.OrderDate),
            "status" => desc ? query.OrderByDescending(n => n.Order!.Status) : query.OrderBy(n => n.Order!.Status),
            "accountnumber" => desc ? query.OrderByDescending(n => n.Order!.Account!.Number) : query.OrderBy(n => n.Order!.Account!.Number),
            "nontradetype" => desc ? query.OrderByDescending(n => n.NonTradeType) : query.OrderBy(n => n.NonTradeType),
            "amount" => desc ? query.OrderByDescending(n => n.Amount) : query.OrderBy(n => n.Amount),
            "currencycode" => desc ? query.OrderByDescending(n => n.Currency!.Code) : query.OrderBy(n => n.Currency!.Code),
            "referencenumber" => desc ? query.OrderByDescending(n => n.ReferenceNumber) : query.OrderBy(n => n.ReferenceNumber),
            "processedat" => desc ? query.OrderByDescending(n => n.ProcessedAt) : query.OrderBy(n => n.ProcessedAt),
            "externalid" => desc ? query.OrderByDescending(n => n.Order!.ExternalId) : query.OrderBy(n => n.Order!.ExternalId),
            _ => query.OrderByDescending(n => n.Order!.CreatedAt)
        };
    }
}
