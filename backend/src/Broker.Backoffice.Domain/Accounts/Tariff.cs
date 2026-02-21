using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Accounts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Tariff
{
    Basic = 0,
    Standard = 1,
    Premium = 2,
    VIP = 3
}
