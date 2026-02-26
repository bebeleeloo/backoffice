using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Orders;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderCategory
{
    Trade = 0,
    NonTrade = 1
}
