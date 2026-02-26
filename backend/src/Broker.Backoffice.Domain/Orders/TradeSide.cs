using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Orders;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TradeSide
{
    Buy = 0,
    Sell = 1,
    ShortSell = 2,
    BuyToCover = 3
}
