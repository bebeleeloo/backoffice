using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Clients;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvestmentTimeHorizon
{
    Short = 0,
    Medium = 1,
    Long = 2
}
