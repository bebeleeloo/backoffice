using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Domain.Transactions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Transactions.NonTradeTransactions;

public sealed record GetNonTradeTransactionsQuery : PagedQuery, IRequest<PagedResult<NonTradeTransactionListItemDto>>
{
    public List<TransactionStatus>? Status { get; init; }
    public List<Guid>? OrderId { get; init; }
    public List<Guid>? AccountId { get; init; }
    public List<Guid>? InstrumentId { get; init; }
    public string? TransactionNumber { get; init; }
    public string? OrderNumber { get; init; }
    public string? CurrencyCode { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? ExternalId { get; init; }
    public DateTime? TransactionDateFrom { get; init; }
    public DateTime? TransactionDateTo { get; init; }
    public DateTime? CreatedFrom { get; init; }
    public DateTime? CreatedTo { get; init; }
    public DateTime? ProcessedFrom { get; init; }
    public DateTime? ProcessedTo { get; init; }
    public decimal? AmountMin { get; init; }
    public decimal? AmountMax { get; init; }
}

public sealed class GetNonTradeTransactionsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetNonTradeTransactionsQuery, PagedResult<NonTradeTransactionListItemDto>>
{
    public async Task<PagedResult<NonTradeTransactionListItemDto>> Handle(GetNonTradeTransactionsQuery request, CancellationToken ct)
    {
        var query = db.NonTradeTransactions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.TransactionNumber))
            query = query.Where(n => EF.Functions.Like(n.Transaction!.TransactionNumber, $"%{request.TransactionNumber}%"));

        if (!string.IsNullOrWhiteSpace(request.OrderNumber))
            query = query.Where(n => n.Transaction!.Order != null && EF.Functions.Like(n.Transaction.Order.OrderNumber, $"%{request.OrderNumber}%"));

        if (!string.IsNullOrWhiteSpace(request.ExternalId))
            query = query.Where(n => n.Transaction!.ExternalId != null && EF.Functions.Like(n.Transaction.ExternalId, $"%{request.ExternalId}%"));

        if (request.OrderId is { Count: > 0 })
            query = query.Where(n => n.Transaction!.OrderId.HasValue && request.OrderId.Contains(n.Transaction.OrderId.Value));

        if (request.AccountId is { Count: > 0 })
            query = query.Where(n => n.Transaction!.Order != null && request.AccountId.Contains(n.Transaction.Order.AccountId));

        if (request.InstrumentId is { Count: > 0 })
            query = query.Where(n => n.InstrumentId != null && request.InstrumentId.Contains(n.InstrumentId.Value));

        if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
            query = query.Where(n => EF.Functions.Like(n.Currency!.Code, $"%{request.CurrencyCode}%"));

        if (!string.IsNullOrWhiteSpace(request.ReferenceNumber))
            query = query.Where(n => n.ReferenceNumber != null && EF.Functions.Like(n.ReferenceNumber, $"%{request.ReferenceNumber}%"));

        if (!string.IsNullOrWhiteSpace(request.Q))
            query = query.Where(n =>
                n.Transaction!.TransactionNumber.Contains(request.Q) ||
                (n.Transaction.Order != null && n.Transaction.Order.OrderNumber.Contains(request.Q)) ||
                (n.Transaction.Order != null && n.Transaction.Order.Account != null && n.Transaction.Order.Account.Number.Contains(request.Q)) ||
                (n.ReferenceNumber != null && n.ReferenceNumber.Contains(request.Q)) ||
                (n.Transaction.ExternalId != null && n.Transaction.ExternalId.Contains(request.Q)));

        if (request.Status is { Count: > 0 })
            query = query.Where(n => request.Status.Contains(n.Transaction!.Status));

        if (request.TransactionDateFrom.HasValue)
            query = query.Where(n => n.Transaction!.TransactionDate >= request.TransactionDateFrom.Value);
        if (request.TransactionDateTo.HasValue)
            query = query.Where(n => n.Transaction!.TransactionDate < request.TransactionDateTo.Value.AddDays(1));

        if (request.CreatedFrom.HasValue)
            query = query.Where(n => n.Transaction!.CreatedAt >= request.CreatedFrom.Value);
        if (request.CreatedTo.HasValue)
            query = query.Where(n => n.Transaction!.CreatedAt < request.CreatedTo.Value.AddDays(1));

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
            .Select(n => new NonTradeTransactionListItemDto(
                n.TransactionId,
                n.Transaction!.Order != null ? n.Transaction.Order.OrderNumber : null,
                n.Transaction.Order != null && n.Transaction.Order.Account != null ? n.Transaction.Order.Account.Number : null,
                n.Transaction.TransactionNumber,
                n.Transaction.Status,
                n.Transaction.TransactionDate,
                n.Amount,
                n.Currency!.Code,
                n.Instrument != null ? n.Instrument.Symbol : null,
                n.Instrument != null ? n.Instrument.Name : null,
                n.ReferenceNumber,
                n.ProcessedAt,
                n.Transaction.ExternalId,
                n.Transaction.CreatedAt,
                n.Transaction.RowVersion))
            .ToListAsync(ct);

        return new PagedResult<NonTradeTransactionListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    private static IQueryable<NonTradeTransaction> ApplySort(IQueryable<NonTradeTransaction> query, string sort)
    {
        var desc = sort.StartsWith('-');
        var prop = (desc ? sort[1..] : sort).ToLowerInvariant();
        return prop switch
        {
            "transactionnumber" => desc ? query.OrderByDescending(n => n.Transaction!.TransactionNumber) : query.OrderBy(n => n.Transaction!.TransactionNumber),
            "ordernumber" => desc ? query.OrderByDescending(n => n.Transaction!.Order != null ? n.Transaction.Order.OrderNumber : null) : query.OrderBy(n => n.Transaction!.Order != null ? n.Transaction.Order.OrderNumber : null),
            "transactiondate" => desc ? query.OrderByDescending(n => n.Transaction!.TransactionDate) : query.OrderBy(n => n.Transaction!.TransactionDate),
            "status" => desc ? query.OrderByDescending(n => n.Transaction!.Status) : query.OrderBy(n => n.Transaction!.Status),
            "accountnumber" => desc ? query.OrderByDescending(n => n.Transaction!.Order != null ? n.Transaction.Order.Account!.Number : null) : query.OrderBy(n => n.Transaction!.Order != null ? n.Transaction.Order.Account!.Number : null),
            "amount" => desc ? query.OrderByDescending(n => n.Amount) : query.OrderBy(n => n.Amount),
            "currencycode" => desc ? query.OrderByDescending(n => n.Currency!.Code) : query.OrderBy(n => n.Currency!.Code),
            "referencenumber" => desc ? query.OrderByDescending(n => n.ReferenceNumber) : query.OrderBy(n => n.ReferenceNumber),
            "processedat" => desc ? query.OrderByDescending(n => n.ProcessedAt) : query.OrderBy(n => n.ProcessedAt),
            "externalid" => desc ? query.OrderByDescending(n => n.Transaction!.ExternalId) : query.OrderBy(n => n.Transaction!.ExternalId),
            _ => query.OrderByDescending(n => n.Transaction!.CreatedAt)
        };
    }
}
