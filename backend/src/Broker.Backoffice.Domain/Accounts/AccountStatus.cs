using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Accounts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccountStatus
{
    Active = 0,
    Blocked = 1,
    Closed = 2,
    Suspended = 3
}
