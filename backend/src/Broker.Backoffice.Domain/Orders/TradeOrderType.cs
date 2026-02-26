using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Orders;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TradeOrderType
{
    Market = 0,
    Limit = 1,
    Stop = 2,
    StopLimit = 3
}
