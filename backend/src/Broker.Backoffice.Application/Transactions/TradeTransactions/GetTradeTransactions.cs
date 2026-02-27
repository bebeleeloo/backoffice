using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Domain.Orders;
using Broker.Backoffice.Domain.Transactions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Transactions.TradeTransactions;

public sealed record GetTradeTransactionsQuery : PagedQuery, IRequest<PagedResult<TradeTransactionListItemDto>>
{
    public List<TransactionStatus>? Status { get; init; }
    public List<TradeSide>? Side { get; init; }
    public List<Guid>? OrderId { get; init; }
    public List<Guid>? AccountId { get; init; }
    public List<Guid>? InstrumentId { get; init; }
    public string? TransactionNumber { get; init; }
    public string? OrderNumber { get; init; }
    public string? ExternalId { get; init; }
    public DateTime? TransactionDateFrom { get; init; }
    public DateTime? TransactionDateTo { get; init; }
    public DateTime? CreatedFrom { get; init; }
    public DateTime? CreatedTo { get; init; }
    public DateTime? SettlementDateFrom { get; init; }
    public DateTime? SettlementDateTo { get; init; }
    public decimal? QuantityMin { get; init; }
    public decimal? QuantityMax { get; init; }
    public decimal? PriceMin { get; init; }
    public decimal? PriceMax { get; init; }
    public decimal? CommissionMin { get; init; }
    public decimal? CommissionMax { get; init; }
}

public sealed class GetTradeTransactionsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetTradeTransactionsQuery, PagedResult<TradeTransactionListItemDto>>
{
    public async Task<PagedResult<TradeTransactionListItemDto>> Handle(GetTradeTransactionsQuery request, CancellationToken ct)
    {
        var query = db.TradeTransactions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.TransactionNumber))
            query = query.Where(t => EF.Functions.Like(t.Transaction!.TransactionNumber, $"%{request.TransactionNumber}%"));

        if (!string.IsNullOrWhiteSpace(request.OrderNumber))
            query = query.Where(t => t.Transaction!.Order != null && EF.Functions.Like(t.Transaction.Order.OrderNumber, $"%{request.OrderNumber}%"));

        if (!string.IsNullOrWhiteSpace(request.ExternalId))
            query = query.Where(t => t.Transaction!.ExternalId != null && EF.Functions.Like(t.Transaction.ExternalId, $"%{request.ExternalId}%"));

        if (request.OrderId is { Count: > 0 })
            query = query.Where(t => t.Transaction!.OrderId.HasValue && request.OrderId.Contains(t.Transaction.OrderId.Value));

        if (request.AccountId is { Count: > 0 })
            query = query.Where(t => t.Transaction!.Order != null && request.AccountId.Contains(t.Transaction.Order.AccountId));

        if (request.InstrumentId is { Count: > 0 })
            query = query.Where(t => request.InstrumentId.Contains(t.InstrumentId));

        if (!string.IsNullOrWhiteSpace(request.Q))
            query = query.Where(t =>
                t.Transaction!.TransactionNumber.Contains(request.Q) ||
                (t.Transaction.Order != null && t.Transaction.Order.OrderNumber.Contains(request.Q)) ||
                (t.Transaction.Order != null && t.Transaction.Order.Account != null && t.Transaction.Order.Account.Number.Contains(request.Q)) ||
                t.Instrument!.Symbol.Contains(request.Q) ||
                (t.Transaction.ExternalId != null && t.Transaction.ExternalId.Contains(request.Q)));

        if (request.Status is { Count: > 0 })
            query = query.Where(t => request.Status.Contains(t.Transaction!.Status));
        if (request.Side is { Count: > 0 })
            query = query.Where(t => request.Side.Contains(t.Side));

        if (request.TransactionDateFrom.HasValue)
            query = query.Where(t => t.Transaction!.TransactionDate >= request.TransactionDateFrom.Value);
        if (request.TransactionDateTo.HasValue)
            query = query.Where(t => t.Transaction!.TransactionDate < request.TransactionDateTo.Value.AddDays(1));

        if (request.CreatedFrom.HasValue)
            query = query.Where(t => t.Transaction!.CreatedAt >= request.CreatedFrom.Value);
        if (request.CreatedTo.HasValue)
            query = query.Where(t => t.Transaction!.CreatedAt < request.CreatedTo.Value.AddDays(1));

        if (request.SettlementDateFrom.HasValue)
            query = query.Where(t => t.SettlementDate >= request.SettlementDateFrom.Value);
        if (request.SettlementDateTo.HasValue)
            query = query.Where(t => t.SettlementDate < request.SettlementDateTo.Value.AddDays(1));

        if (request.QuantityMin.HasValue)
            query = query.Where(t => t.Quantity >= request.QuantityMin.Value);
        if (request.QuantityMax.HasValue)
            query = query.Where(t => t.Quantity <= request.QuantityMax.Value);

        if (request.PriceMin.HasValue)
            query = query.Where(t => t.Price >= request.PriceMin.Value);
        if (request.PriceMax.HasValue)
            query = query.Where(t => t.Price <= request.PriceMax.Value);

        if (request.CommissionMin.HasValue)
            query = query.Where(t => t.Commission >= request.CommissionMin.Value);
        if (request.CommissionMax.HasValue)
            query = query.Where(t => t.Commission <= request.CommissionMax.Value);

        query = ApplySort(query, request.Sort ?? "-CreatedAt");

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TradeTransactionListItemDto(
                t.TransactionId,
                t.Transaction!.Order != null ? t.Transaction.Order.OrderNumber : null,
                t.Transaction.Order != null && t.Transaction.Order.Account != null ? t.Transaction.Order.Account.Number : null,
                t.Transaction.TransactionNumber,
                t.Transaction.Status,
                t.Transaction.TransactionDate,
                t.Instrument!.Symbol,
                t.Instrument.Name,
                t.Side,
                t.Quantity,
                t.Price,
                t.Commission,
                t.SettlementDate,
                t.Venue,
                t.Transaction.ExternalId,
                t.Transaction.CreatedAt,
                t.Transaction.RowVersion))
            .ToListAsync(ct);

        return new PagedResult<TradeTransactionListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    private static IQueryable<TradeTransaction> ApplySort(IQueryable<TradeTransaction> query, string sort)
    {
        var desc = sort.StartsWith('-');
        var prop = (desc ? sort[1..] : sort).ToLowerInvariant();
        return prop switch
        {
            "transactionnumber" => desc ? query.OrderByDescending(t => t.Transaction!.TransactionNumber) : query.OrderBy(t => t.Transaction!.TransactionNumber),
            "ordernumber" => desc ? query.OrderByDescending(t => t.Transaction!.Order != null ? t.Transaction.Order.OrderNumber : null) : query.OrderBy(t => t.Transaction!.Order != null ? t.Transaction.Order.OrderNumber : null),
            "transactiondate" => desc ? query.OrderByDescending(t => t.Transaction!.TransactionDate) : query.OrderBy(t => t.Transaction!.TransactionDate),
            "status" => desc ? query.OrderByDescending(t => t.Transaction!.Status) : query.OrderBy(t => t.Transaction!.Status),
            "accountnumber" => desc ? query.OrderByDescending(t => t.Transaction!.Order != null ? t.Transaction.Order.Account!.Number : null) : query.OrderBy(t => t.Transaction!.Order != null ? t.Transaction.Order.Account!.Number : null),
            "instrumentsymbol" => desc ? query.OrderByDescending(t => t.Instrument!.Symbol) : query.OrderBy(t => t.Instrument!.Symbol),
            "side" => desc ? query.OrderByDescending(t => t.Side) : query.OrderBy(t => t.Side),
            "quantity" => desc ? query.OrderByDescending(t => t.Quantity) : query.OrderBy(t => t.Quantity),
            "price" => desc ? query.OrderByDescending(t => t.Price) : query.OrderBy(t => t.Price),
            "commission" => desc ? query.OrderByDescending(t => t.Commission) : query.OrderBy(t => t.Commission),
            "settlementdate" => desc ? query.OrderByDescending(t => t.SettlementDate) : query.OrderBy(t => t.SettlementDate),
            "venue" => desc ? query.OrderByDescending(t => t.Venue) : query.OrderBy(t => t.Venue),
            "externalid" => desc ? query.OrderByDescending(t => t.Transaction!.ExternalId) : query.OrderBy(t => t.Transaction!.ExternalId),
            _ => query.OrderByDescending(t => t.Transaction!.CreatedAt)
        };
    }
}
