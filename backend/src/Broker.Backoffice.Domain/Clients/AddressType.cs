using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Clients;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AddressType
{
    Legal = 0,
    Mailing = 1,
    Working = 2
}
