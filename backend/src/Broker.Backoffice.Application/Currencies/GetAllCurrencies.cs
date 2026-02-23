using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Currencies;

public sealed record GetAllCurrenciesQuery : IRequest<IReadOnlyList<CurrencyDto>>;

public sealed class GetAllCurrenciesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetAllCurrenciesQuery, IReadOnlyList<CurrencyDto>>
{
    public async Task<IReadOnlyList<CurrencyDto>> Handle(GetAllCurrenciesQuery request, CancellationToken ct)
    {
        return await db.Currencies
            .OrderBy(c => c.Code)
            .Select(c => new CurrencyDto(c.Id, c.Code, c.Name, c.Symbol, c.IsActive))
            .ToListAsync(ct);
    }
}
