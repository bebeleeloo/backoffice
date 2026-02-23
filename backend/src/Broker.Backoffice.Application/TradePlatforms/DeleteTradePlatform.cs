using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.TradePlatforms;

public sealed record DeleteTradePlatformCommand(Guid Id) : IRequest;

public sealed class DeleteTradePlatformCommandHandler(IAppDbContext db)
    : IRequestHandler<DeleteTradePlatformCommand>
{
    public async Task Handle(DeleteTradePlatformCommand request, CancellationToken ct)
    {
        var entity = await db.TradePlatforms.FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Trade platform {request.Id} not found");

        db.TradePlatforms.Remove(entity);

        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { throw new InvalidOperationException("Cannot delete: this trade platform is in use"); }
    }
}
