using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Domain.Instruments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Instruments;

public sealed record GetInstrumentsQuery : PagedQuery, IRequest<PagedResult<InstrumentListItemDto>>
{
    public string? Symbol { get; init; }
    public string? Name { get; init; }
    public List<InstrumentType>? Type { get; init; }
    public List<AssetClass>? AssetClass { get; init; }
    public List<InstrumentStatus>? Status { get; init; }
    public List<Sector>? Sector { get; init; }
    public string? ExchangeName { get; init; }
    public string? CurrencyCode { get; init; }
    public bool? IsMarginEligible { get; init; }
}

public sealed class GetInstrumentsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetInstrumentsQuery, PagedResult<InstrumentListItemDto>>
{
    public async Task<PagedResult<InstrumentListItemDto>> Handle(GetInstrumentsQuery request, CancellationToken ct)
    {
        var query = db.Instruments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Symbol))
            query = query.Where(i => EF.Functions.Like(i.Symbol, $"%{request.Symbol}%"));

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(i => EF.Functions.Like(i.Name, $"%{request.Name}%"));

        if (!string.IsNullOrWhiteSpace(request.ExchangeName))
            query = query.Where(i => i.Exchange != null &&
                (EF.Functions.Like(i.Exchange.Code, $"%{request.ExchangeName}%") ||
                 EF.Functions.Like(i.Exchange.Name, $"%{request.ExchangeName}%")));

        if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
            query = query.Where(i => i.Currency != null &&
                EF.Functions.Like(i.Currency.Code, $"%{request.CurrencyCode}%"));

        if (!string.IsNullOrWhiteSpace(request.Q))
            query = query.Where(i =>
                i.Symbol.Contains(request.Q) ||
                i.Name.Contains(request.Q) ||
                (i.ISIN != null && i.ISIN.Contains(request.Q)) ||
                (i.CUSIP != null && i.CUSIP.Contains(request.Q)) ||
                (i.ExternalId != null && i.ExternalId.Contains(request.Q)));

        if (request.Type is { Count: > 0 })
            query = query.Where(i => request.Type.Contains(i.Type));
        if (request.AssetClass is { Count: > 0 })
            query = query.Where(i => request.AssetClass.Contains(i.AssetClass));
        if (request.Status is { Count: > 0 })
            query = query.Where(i => request.Status.Contains(i.Status));
        if (request.Sector is { Count: > 0 })
            query = query.Where(i => i.Sector != null && request.Sector.Contains(i.Sector.Value));
        if (request.IsMarginEligible.HasValue)
            query = query.Where(i => i.IsMarginEligible == request.IsMarginEligible.Value);

        var projected = query.SortBy(request.Sort ?? "Symbol")
            .Select(i => new InstrumentListItemDto(
                i.Id,
                i.Symbol,
                i.Name,
                i.ISIN,
                i.CUSIP,
                i.Type,
                i.AssetClass,
                i.Status,
                i.Exchange != null ? i.Exchange.Code : null,
                i.Exchange != null ? i.Exchange.Name : null,
                i.Currency != null ? i.Currency.Code : null,
                i.Country != null ? i.Country.Name : null,
                i.Country != null ? i.Country.FlagEmoji : null,
                i.Sector,
                i.LotSize,
                i.IsMarginEligible,
                i.ExternalId,
                i.CreatedAt,
                i.RowVersion));

        return await projected.ToPagedResultAsync(request.Page, request.PageSize, ct);
    }
}
