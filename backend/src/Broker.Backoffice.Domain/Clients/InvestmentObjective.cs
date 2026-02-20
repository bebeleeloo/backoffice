using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Clients;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvestmentObjective
{
    Preservation = 0,
    Income = 1,
    Growth = 2,
    Speculation = 3,
    Hedging = 4,
    Other = 5
}
