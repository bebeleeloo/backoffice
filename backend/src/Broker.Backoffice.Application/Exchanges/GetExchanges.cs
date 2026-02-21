using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Exchanges;

public sealed record ExchangeDto(Guid Id, string Code, string Name, Guid? CountryId, bool IsActive);

public sealed record GetExchangesQuery : IRequest<IReadOnlyList<ExchangeDto>>;

public sealed class GetExchangesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetExchangesQuery, IReadOnlyList<ExchangeDto>>
{
    public async Task<IReadOnlyList<ExchangeDto>> Handle(GetExchangesQuery request, CancellationToken ct)
    {
        return await db.Exchanges
            .Where(e => e.IsActive)
            .OrderBy(e => e.Code)
            .Select(e => new ExchangeDto(e.Id, e.Code, e.Name, e.CountryId, e.IsActive))
            .ToListAsync(ct);
    }
}
