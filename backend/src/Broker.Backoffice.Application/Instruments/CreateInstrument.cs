using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Instruments;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Instruments;

public sealed record CreateInstrumentCommand(
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
    string? ExternalId) : IRequest<InstrumentDto>;

public sealed class CreateInstrumentCommandValidator : AbstractValidator<CreateInstrumentCommand>
{
    public CreateInstrumentCommandValidator()
    {
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ISIN).MaximumLength(12);
        RuleFor(x => x.CUSIP).MaximumLength(9);
        RuleFor(x => x.IssuerName).MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.ExternalId).MaximumLength(64);
    }
}

public sealed class CreateInstrumentCommandHandler(
    IAppDbContext db,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    IAuditContext audit) : IRequestHandler<CreateInstrumentCommand, InstrumentDto>
{
    public async Task<InstrumentDto> Handle(CreateInstrumentCommand request, CancellationToken ct)
    {
        if (await db.Instruments.AnyAsync(i => i.Symbol == request.Symbol, ct))
            throw new InvalidOperationException($"Instrument with symbol '{request.Symbol}' already exists");

        var instrument = new Instrument
        {
            Id = Guid.NewGuid(),
            Symbol = request.Symbol,
            Name = request.Name,
            ISIN = request.ISIN,
            CUSIP = request.CUSIP,
            Type = request.Type,
            AssetClass = request.AssetClass,
            Status = request.Status,
            ExchangeId = request.ExchangeId,
            CurrencyId = request.CurrencyId,
            CountryId = request.CountryId,
            Sector = request.Sector,
            LotSize = request.LotSize,
            TickSize = request.TickSize,
            MarginRequirement = request.MarginRequirement,
            IsMarginEligible = request.IsMarginEligible,
            ListingDate = request.ListingDate,
            DelistingDate = request.DelistingDate,
            ExpirationDate = request.ExpirationDate,
            IssuerName = request.IssuerName,
            Description = request.Description,
            ExternalId = request.ExternalId,
            CreatedAt = clock.UtcNow,
            CreatedBy = currentUser.UserName
        };

        db.Instruments.Add(instrument);
        await db.SaveChangesAsync(ct);

        await db.Instruments.Entry(instrument).Reference(x => x.Exchange).LoadAsync(ct);
        await db.Instruments.Entry(instrument).Reference(x => x.Currency).LoadAsync(ct);
        await db.Instruments.Entry(instrument).Reference(x => x.Country).LoadAsync(ct);

        audit.EntityType = "Instrument";
        audit.EntityId = instrument.Id.ToString();
        audit.AfterJson = JsonSerializer.Serialize(new { instrument.Id, instrument.Symbol, instrument.Name, instrument.Status });

        return GetInstrumentByIdQueryHandler.ToDto(instrument);
    }
}
