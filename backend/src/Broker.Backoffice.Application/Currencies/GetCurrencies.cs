using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Currencies;

public sealed record CurrencyDto(Guid Id, string Code, string Name, string? Symbol, bool IsActive);

public sealed record GetCurrenciesQuery : IRequest<IReadOnlyList<CurrencyDto>>;

public sealed class GetCurrenciesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetCurrenciesQuery, IReadOnlyList<CurrencyDto>>
{
    public async Task<IReadOnlyList<CurrencyDto>> Handle(GetCurrenciesQuery request, CancellationToken ct)
    {
        return await db.Currencies
            .Where(c => c.IsActive)
            .OrderBy(c => c.Code)
            .Select(c => new CurrencyDto(c.Id, c.Code, c.Name, c.Symbol, c.IsActive))
            .ToListAsync(ct);
    }
}
