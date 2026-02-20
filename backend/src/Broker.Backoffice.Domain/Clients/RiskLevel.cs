using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Clients;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RiskLevel
{
    Low = 0,
    Medium = 1,
    High = 2
}
