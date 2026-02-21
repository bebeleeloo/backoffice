using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Instruments;

public sealed record DeleteInstrumentCommand(Guid Id) : IRequest;

public sealed class DeleteInstrumentCommandHandler(
    IAppDbContext db,
    IAuditContext audit) : IRequestHandler<DeleteInstrumentCommand>
{
    public async Task Handle(DeleteInstrumentCommand request, CancellationToken ct)
    {
        var instrument = await db.Instruments.FirstOrDefaultAsync(i => i.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Instrument {request.Id} not found");

        audit.EntityType = "Instrument";
        audit.EntityId = instrument.Id.ToString();
        audit.BeforeJson = JsonSerializer.Serialize(new { instrument.Id, instrument.Symbol, instrument.Name, instrument.Status });

        db.Instruments.Remove(instrument);
        await db.SaveChangesAsync(ct);
    }
}
