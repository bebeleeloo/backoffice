using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Accounts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DeliveryType
{
    Paper = 0,
    Electronic = 1
}
