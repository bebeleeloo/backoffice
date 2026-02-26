using Broker.Backoffice.Domain.Common;
using Broker.Backoffice.Domain.Countries;

namespace Broker.Backoffice.Domain.Clients;

public sealed class ClientAddress : Entity
{
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public AddressType Type { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public Guid CountryId { get; set; }
    public Country Country { get; set; } = null!;
}
