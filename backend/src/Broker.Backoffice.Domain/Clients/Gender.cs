using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Clients;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Gender
{
    Male = 0,
    Female = 1,
    Other = 2,
    Unspecified = 3
}
