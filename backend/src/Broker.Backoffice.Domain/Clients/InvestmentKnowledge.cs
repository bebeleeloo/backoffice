using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Clients;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvestmentKnowledge
{
    None = 0,
    Basic = 1,
    Good = 2,
    Advanced = 3
}
