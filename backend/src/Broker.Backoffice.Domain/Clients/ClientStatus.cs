using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Clients;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ClientStatus
{
    Active = 0,
    Blocked = 1,
    PendingKyc = 2
}
