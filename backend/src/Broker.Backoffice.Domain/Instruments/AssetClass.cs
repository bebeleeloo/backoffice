using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Instruments;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssetClass
{
    Equities = 0,
    FixedIncome = 1,
    Derivatives = 2,
    ForeignExchange = 3,
    Commodities = 4,
    Funds = 5
}
