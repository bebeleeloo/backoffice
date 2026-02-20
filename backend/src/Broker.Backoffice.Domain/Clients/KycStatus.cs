using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Clients;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum KycStatus
{
    NotStarted = 0,
    InProgress = 1,
    Approved = 2,
    Rejected = 3
}
