using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Countries;

public sealed record CountryDto(Guid Id, string Iso2, string? Iso3, string Name, string FlagEmoji, bool IsActive);

public sealed record GetCountriesQuery : IRequest<IReadOnlyList<CountryDto>>;

public sealed class GetCountriesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetCountriesQuery, IReadOnlyList<CountryDto>>
{
    public async Task<IReadOnlyList<CountryDto>> Handle(GetCountriesQuery request, CancellationToken ct)
    {
        return await db.Countries
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CountryDto(c.Id, c.Iso2, c.Iso3, c.Name, c.FlagEmoji, c.IsActive))
            .ToListAsync(ct);
    }
}
