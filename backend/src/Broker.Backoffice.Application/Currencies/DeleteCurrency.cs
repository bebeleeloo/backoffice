using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Currencies;

public sealed record DeleteCurrencyCommand(Guid Id) : IRequest;

public sealed class DeleteCurrencyCommandHandler(IAppDbContext db)
    : IRequestHandler<DeleteCurrencyCommand>
{
    public async Task Handle(DeleteCurrencyCommand request, CancellationToken ct)
    {
        var entity = await db.Currencies.FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Currency {request.Id} not found");

        db.Currencies.Remove(entity);

        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { throw new InvalidOperationException("Cannot delete: this currency is in use"); }
    }
}
