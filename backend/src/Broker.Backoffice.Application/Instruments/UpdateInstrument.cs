using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Instruments;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Instruments;

public sealed record UpdateInstrumentCommand(
    Guid Id,
    string Symbol,
    string Name,
    string? ISIN,
    string? CUSIP,
    InstrumentType Type,
    AssetClass AssetClass,
    InstrumentStatus Status,
    Guid? ExchangeId,
    Guid? CurrencyId,
    Guid? CountryId,
    Sector? Sector,
    int LotSize,
    decimal? TickSize,
    decimal? MarginRequirement,
    bool IsMarginEligible,
    DateTime? ListingDate,
    DateTime? DelistingDate,
    DateTime? ExpirationDate,
    string? IssuerName,
    string? Description,
    string? ExternalId,
    byte[] RowVersion) : IRequest<InstrumentDto>;

public sealed class UpdateInstrumentCommandValidator : AbstractValidator<UpdateInstrumentCommand>
{
    public UpdateInstrumentCommandValidator()
    {
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ISIN).MaximumLength(12);
        RuleFor(x => x.CUSIP).MaximumLength(9);
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.IssuerName).MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.ExternalId).MaximumLength(64);
    }
}

public sealed class UpdateInstrumentCommandHandler(
    IAppDbContext db,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    IAuditContext audit) : IRequestHandler<UpdateInstrumentCommand, InstrumentDto>
{
    public async Task<InstrumentDto> Handle(UpdateInstrumentCommand request, CancellationToken ct)
    {
        var instrument = await db.Instruments
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Instrument {request.Id} not found");

        if (await db.Instruments.AnyAsync(i => i.Symbol == request.Symbol && i.Id != request.Id, ct))
            throw new InvalidOperationException($"Instrument with symbol '{request.Symbol}' already exists");

        var before = JsonSerializer.Serialize(new { instrument.Id, instrument.Symbol, instrument.Name, instrument.Status });
        db.Instruments.Entry(instrument).Property(i => i.RowVersion).OriginalValue = request.RowVersion;

        instrument.Symbol = request.Symbol;
        instrument.Name = request.Name;
        instrument.ISIN = request.ISIN;
        instrument.CUSIP = request.CUSIP;
        instrument.Type = request.Type;
        instrument.AssetClass = request.AssetClass;
        instrument.Status = request.Status;
        instrument.ExchangeId = request.ExchangeId;
        instrument.CurrencyId = request.CurrencyId;
        instrument.CountryId = request.CountryId;
        instrument.Sector = request.Sector;
        instrument.LotSize = request.LotSize;
        instrument.TickSize = request.TickSize;
        instrument.MarginRequirement = request.MarginRequirement;
        instrument.IsMarginEligible = request.IsMarginEligible;
        instrument.ListingDate = request.ListingDate;
        instrument.DelistingDate = request.DelistingDate;
        instrument.ExpirationDate = request.ExpirationDate;
        instrument.IssuerName = request.IssuerName;
        instrument.Description = request.Description;
        instrument.ExternalId = request.ExternalId;
        instrument.UpdatedAt = clock.UtcNow;
        instrument.UpdatedBy = currentUser.UserName;

        await db.SaveChangesAsync(ct);

        var updated = await db.Instruments
            .Include(i => i.Exchange)
            .Include(i => i.Currency)
            .Include(i => i.Country)
            .FirstAsync(i => i.Id == instrument.Id, ct);

        audit.EntityType = "Instrument";
        audit.EntityId = instrument.Id.ToString();
        audit.BeforeJson = before;
        audit.AfterJson = JsonSerializer.Serialize(new { updated.Id, updated.Symbol, updated.Name, updated.Status });

        return GetInstrumentByIdQueryHandler.ToDto(updated);
    }
}
