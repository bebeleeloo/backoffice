using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Clearers;

public sealed record ClearerDto(Guid Id, string Name, string? Description, bool IsActive);

public sealed record GetClearersQuery : IRequest<IReadOnlyList<ClearerDto>>;

public sealed class GetClearersQueryHandler(IAppDbContext db)
    : IRequestHandler<GetClearersQuery, IReadOnlyList<ClearerDto>>
{
    public async Task<IReadOnlyList<ClearerDto>> Handle(GetClearersQuery request, CancellationToken ct)
    {
        return await db.Clearers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new ClearerDto(c.Id, c.Name, c.Description, c.IsActive))
            .ToListAsync(ct);
    }
}
