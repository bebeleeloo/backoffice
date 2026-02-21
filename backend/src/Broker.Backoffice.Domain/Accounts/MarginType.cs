using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Accounts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MarginType
{
    Cash = 0,
    MarginX1 = 1,
    MarginX2 = 2,
    MarginX4 = 3,
    DayTrader = 4
}
