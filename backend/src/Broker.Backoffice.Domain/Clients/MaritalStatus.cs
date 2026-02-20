using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Clients;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MaritalStatus
{
    Single = 0,
    Married = 1,
    Divorced = 2,
    Widowed = 3,
    Separated = 4,
    CivilUnion = 5,
    Unspecified = 6
}
