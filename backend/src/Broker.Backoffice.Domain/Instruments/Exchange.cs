using Broker.Backoffice.Domain.Countries;

namespace Broker.Backoffice.Domain.Instruments;

public sealed class Exchange
{
    public Guid Id { get; init; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? CountryId { get; set; }
    public Country? Country { get; set; }
    public bool IsActive { get; set; } = true;
}
