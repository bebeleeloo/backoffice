using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Orders.TradeOrders;

public sealed record GetTradeOrdersQuery : PagedQuery, IRequest<PagedResult<TradeOrderListItemDto>>
{
    public List<OrderStatus>? Status { get; init; }
    public List<TradeSide>? Side { get; init; }
    public List<TradeOrderType>? OrderType { get; init; }
    public List<Guid>? AccountId { get; init; }
    public List<Guid>? InstrumentId { get; init; }
    public string? OrderNumber { get; init; }
    public List<TimeInForce>? TimeInForce { get; init; }
    public string? ExternalId { get; init; }
    public DateTime? OrderDateFrom { get; init; }
    public DateTime? OrderDateTo { get; init; }
    public DateTime? CreatedFrom { get; init; }
    public DateTime? CreatedTo { get; init; }
    public DateTime? ExecutedFrom { get; init; }
    public DateTime? ExecutedTo { get; init; }
    public decimal? QuantityMin { get; init; }
    public decimal? QuantityMax { get; init; }
    public decimal? PriceMin { get; init; }
    public decimal? PriceMax { get; init; }
    public decimal? ExecutedQuantityMin { get; init; }
    public decimal? ExecutedQuantityMax { get; init; }
    public decimal? AveragePriceMin { get; init; }
    public decimal? AveragePriceMax { get; init; }
    public decimal? CommissionMin { get; init; }
    public decimal? CommissionMax { get; init; }
}

public sealed class GetTradeOrdersQueryHandler(IAppDbContext db)
    : IRequestHandler<GetTradeOrdersQuery, PagedResult<TradeOrderListItemDto>>
{
    public async Task<PagedResult<TradeOrderListItemDto>> Handle(GetTradeOrdersQuery request, CancellationToken ct)
    {
        var query = db.TradeOrders
            .Include(t => t.Order!).ThenInclude(o => o.Account!)
            .Include(t => t.Instrument!)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.OrderNumber))
            query = query.Where(t => EF.Functions.Like(t.Order!.OrderNumber, $"%{request.OrderNumber}%"));

        if (!string.IsNullOrWhiteSpace(request.ExternalId))
            query = query.Where(t => t.Order!.ExternalId != null && EF.Functions.Like(t.Order.ExternalId, $"%{request.ExternalId}%"));

        if (request.AccountId is { Count: > 0 })
            query = query.Where(t => request.AccountId.Contains(t.Order!.AccountId));

        if (request.InstrumentId is { Count: > 0 })
            query = query.Where(t => request.InstrumentId.Contains(t.InstrumentId));

        if (!string.IsNullOrWhiteSpace(request.Q))
            query = query.Where(t =>
                t.Order!.OrderNumber.Contains(request.Q) ||
                t.Order.Account!.Number.Contains(request.Q) ||
                t.Instrument!.Symbol.Contains(request.Q) ||
                (t.Order.ExternalId != null && t.Order.ExternalId.Contains(request.Q)));

        if (request.Status is { Count: > 0 })
            query = query.Where(t => request.Status.Contains(t.Order!.Status));
        if (request.Side is { Count: > 0 })
            query = query.Where(t => request.Side.Contains(t.Side));
        if (request.OrderType is { Count: > 0 })
            query = query.Where(t => request.OrderType.Contains(t.OrderType));
        if (request.TimeInForce is { Count: > 0 })
            query = query.Where(t => request.TimeInForce.Contains(t.TimeInForce));

        if (request.OrderDateFrom.HasValue)
            query = query.Where(t => t.Order!.OrderDate >= request.OrderDateFrom.Value);
        if (request.OrderDateTo.HasValue)
            query = query.Where(t => t.Order!.OrderDate < request.OrderDateTo.Value.AddDays(1));

        if (request.CreatedFrom.HasValue)
            query = query.Where(t => t.Order!.CreatedAt >= request.CreatedFrom.Value);
        if (request.CreatedTo.HasValue)
            query = query.Where(t => t.Order!.CreatedAt < request.CreatedTo.Value.AddDays(1));

        if (request.ExecutedFrom.HasValue)
            query = query.Where(t => t.ExecutedAt >= request.ExecutedFrom.Value);
        if (request.ExecutedTo.HasValue)
            query = query.Where(t => t.ExecutedAt < request.ExecutedTo.Value.AddDays(1));

        if (request.QuantityMin.HasValue)
            query = query.Where(t => t.Quantity >= request.QuantityMin.Value);
        if (request.QuantityMax.HasValue)
            query = query.Where(t => t.Quantity <= request.QuantityMax.Value);

        if (request.PriceMin.HasValue)
            query = query.Where(t => t.Price >= request.PriceMin.Value);
        if (request.PriceMax.HasValue)
            query = query.Where(t => t.Price <= request.PriceMax.Value);

        if (request.ExecutedQuantityMin.HasValue)
            query = query.Where(t => t.ExecutedQuantity >= request.ExecutedQuantityMin.Value);
        if (request.ExecutedQuantityMax.HasValue)
            query = query.Where(t => t.ExecutedQuantity <= request.ExecutedQuantityMax.Value);

        if (request.AveragePriceMin.HasValue)
            query = query.Where(t => t.AveragePrice >= request.AveragePriceMin.Value);
        if (request.AveragePriceMax.HasValue)
            query = query.Where(t => t.AveragePrice <= request.AveragePriceMax.Value);

        if (request.CommissionMin.HasValue)
            query = query.Where(t => t.Commission >= request.CommissionMin.Value);
        if (request.CommissionMax.HasValue)
            query = query.Where(t => t.Commission <= request.CommissionMax.Value);

        query = ApplySort(query, request.Sort ?? "-CreatedAt");

        var totalCount = await query.CountAsync(ct);
        var entities = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var items = entities.Select(t => new TradeOrderListItemDto(
            t.OrderId,
            t.Order!.Account?.Number ?? "",
            t.Order.OrderNumber,
            t.Order.Status,
            t.Order.OrderDate,
            t.Instrument!.Symbol,
            t.Instrument.Name,
            t.Side,
            t.OrderType,
            t.TimeInForce,
            t.Quantity,
            t.Price,
            t.ExecutedQuantity,
            t.AveragePrice,
            t.Commission,
            t.ExecutedAt,
            t.Order.ExternalId,
            t.Order.CreatedAt,
            t.Order.RowVersion)).ToList();

        return new PagedResult<TradeOrderListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    private static IQueryable<TradeOrder> ApplySort(IQueryable<TradeOrder> query, string sort)
    {
        var desc = sort.StartsWith('-');
        var prop = (desc ? sort[1..] : sort).ToLowerInvariant();
        return prop switch
        {
            "ordernumber" => desc ? query.OrderByDescending(t => t.Order!.OrderNumber) : query.OrderBy(t => t.Order!.OrderNumber),
            "orderdate" => desc ? query.OrderByDescending(t => t.Order!.OrderDate) : query.OrderBy(t => t.Order!.OrderDate),
            "status" => desc ? query.OrderByDescending(t => t.Order!.Status) : query.OrderBy(t => t.Order!.Status),
            "accountnumber" => desc ? query.OrderByDescending(t => t.Order!.Account!.Number) : query.OrderBy(t => t.Order!.Account!.Number),
            "instrumentsymbol" => desc ? query.OrderByDescending(t => t.Instrument!.Symbol) : query.OrderBy(t => t.Instrument!.Symbol),
            "side" => desc ? query.OrderByDescending(t => t.Side) : query.OrderBy(t => t.Side),
            "ordertype" => desc ? query.OrderByDescending(t => t.OrderType) : query.OrderBy(t => t.OrderType),
            "quantity" => desc ? query.OrderByDescending(t => t.Quantity) : query.OrderBy(t => t.Quantity),
            "price" => desc ? query.OrderByDescending(t => t.Price) : query.OrderBy(t => t.Price),
            "executedquantity" => desc ? query.OrderByDescending(t => t.ExecutedQuantity) : query.OrderBy(t => t.ExecutedQuantity),
            "commission" => desc ? query.OrderByDescending(t => t.Commission) : query.OrderBy(t => t.Commission),
            "executedat" => desc ? query.OrderByDescending(t => t.ExecutedAt) : query.OrderBy(t => t.ExecutedAt),
            "externalid" => desc ? query.OrderByDescending(t => t.Order!.ExternalId) : query.OrderBy(t => t.Order!.ExternalId),
            _ => query.OrderByDescending(t => t.Order!.CreatedAt)
        };
    }
}
