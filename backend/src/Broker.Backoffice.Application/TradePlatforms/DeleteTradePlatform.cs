using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.TradePlatforms;

public sealed record DeleteTradePlatformCommand(Guid Id) : IRequest;

public sealed class DeleteTradePlatformCommandHandler(IAppDbContext db, IAuditContext audit)
    : IRequestHandler<DeleteTradePlatformCommand>
{
    public async Task Handle(DeleteTradePlatformCommand request, CancellationToken ct)
    {
        var entity = await db.TradePlatforms.FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Trade platform {request.Id} not found");

        audit.EntityType = "TradePlatform";
        audit.EntityId = entity.Id.ToString();
        audit.BeforeJson = JsonSerializer.Serialize(new { entity.Id, entity.Name, entity.Description, entity.IsActive });

        db.TradePlatforms.Remove(entity);

        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { throw new InvalidOperationException("Cannot delete: this trade platform is in use"); }
    }
}
