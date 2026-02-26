using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Orders;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TimeInForce
{
    Day = 0,
    GTC = 1,
    IOC = 2,
    FOK = 3,
    GTD = 4
}
