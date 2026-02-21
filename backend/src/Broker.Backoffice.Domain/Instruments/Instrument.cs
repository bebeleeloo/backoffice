using Broker.Backoffice.Domain.Common;
using Broker.Backoffice.Domain.Countries;

namespace Broker.Backoffice.Domain.Instruments;

public sealed class Instrument : AuditableEntity
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ISIN { get; set; }
    public string? CUSIP { get; set; }

    public InstrumentType Type { get; set; }
    public AssetClass AssetClass { get; set; }
    public InstrumentStatus Status { get; set; }

    public Guid? ExchangeId { get; set; }
    public Exchange? Exchange { get; set; }

    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public Guid? CountryId { get; set; }
    public Country? Country { get; set; }

    public Sector? Sector { get; set; }
    public int LotSize { get; set; } = 1;
    public decimal? TickSize { get; set; }
    public decimal? MarginRequirement { get; set; }
    public bool IsMarginEligible { get; set; } = true;

    public DateTime? ListingDate { get; set; }
    public DateTime? DelistingDate { get; set; }
    public DateTime? ExpirationDate { get; set; }

    public string? IssuerName { get; set; }
    public string? Description { get; set; }
    public string? ExternalId { get; set; }
}
