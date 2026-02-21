using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Instruments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Instruments;

public sealed record GetInstrumentByIdQuery(Guid Id) : IRequest<InstrumentDto>;

public sealed class GetInstrumentByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetInstrumentByIdQuery, InstrumentDto>
{
    public async Task<InstrumentDto> Handle(GetInstrumentByIdQuery request, CancellationToken ct)
    {
        var i = await db.Instruments
            .Include(x => x.Exchange)
            .Include(x => x.Currency)
            .Include(x => x.Country)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Instrument {request.Id} not found");

        return ToDto(i);
    }

    internal static InstrumentDto ToDto(Instrument i) => new(
        i.Id, i.Symbol, i.Name, i.ISIN, i.CUSIP,
        i.Type, i.AssetClass, i.Status,
        i.ExchangeId, i.Exchange?.Code, i.Exchange?.Name,
        i.CurrencyId, i.Currency?.Code,
        i.CountryId, i.Country?.Name, i.Country?.FlagEmoji,
        i.Sector, i.LotSize, i.TickSize, i.MarginRequirement, i.IsMarginEligible,
        i.ListingDate, i.DelistingDate, i.ExpirationDate,
        i.IssuerName, i.Description, i.ExternalId,
        i.CreatedAt, i.RowVersion);
}
