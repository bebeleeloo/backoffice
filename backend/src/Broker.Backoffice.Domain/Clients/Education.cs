using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Clients;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Education
{
    None = 0,
    HighSchool = 1,
    Bachelor = 2,
    Master = 3,
    PhD = 4,
    Other = 5,
    Unspecified = 6
}
