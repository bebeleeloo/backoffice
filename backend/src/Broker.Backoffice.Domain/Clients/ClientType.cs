using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Clients;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ClientType
{
    Individual = 0,
    Corporate = 1
}
