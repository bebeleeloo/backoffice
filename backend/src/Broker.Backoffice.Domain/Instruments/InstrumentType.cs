using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Instruments;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InstrumentType
{
    Stock = 0,
    Bond = 1,
    ETF = 2,
    Option = 3,
    Future = 4,
    Forex = 5,
    CFD = 6,
    MutualFund = 7,
    Warrant = 8,
    Index = 9
}
