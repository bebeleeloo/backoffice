using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Clearers;

public sealed record DeleteClearerCommand(Guid Id) : IRequest;

public sealed class DeleteClearerCommandHandler(IAppDbContext db)
    : IRequestHandler<DeleteClearerCommand>
{
    public async Task Handle(DeleteClearerCommand request, CancellationToken ct)
    {
        var entity = await db.Clearers.FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Clearer {request.Id} not found");

        db.Clearers.Remove(entity);

        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { throw new InvalidOperationException("Cannot delete: this clearer is in use"); }
    }
}
