using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Clients;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvestmentExperience
{
    None = 0,
    LessThan1Year = 1,
    OneToThreeYears = 2,
    ThreeToFiveYears = 3,
    MoreThan5Years = 4
}
