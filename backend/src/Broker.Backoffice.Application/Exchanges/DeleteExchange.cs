using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Exchanges;

public sealed record DeleteExchangeCommand(Guid Id) : IRequest;

public sealed class DeleteExchangeCommandHandler(IAppDbContext db, IAuditContext audit)
    : IRequestHandler<DeleteExchangeCommand>
{
    public async Task Handle(DeleteExchangeCommand request, CancellationToken ct)
    {
        var entity = await db.Exchanges.FirstOrDefaultAsync(e => e.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Exchange {request.Id} not found");

        audit.EntityType = "Exchange";
        audit.EntityId = entity.Id.ToString();
        audit.BeforeJson = JsonSerializer.Serialize(new { entity.Id, entity.Code, entity.Name, entity.CountryId, entity.IsActive });

        db.Exchanges.Remove(entity);

        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { throw new InvalidOperationException("Cannot delete: this exchange is in use"); }
    }
}
