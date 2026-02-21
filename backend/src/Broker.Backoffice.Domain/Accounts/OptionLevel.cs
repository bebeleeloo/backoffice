using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Accounts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OptionLevel
{
    Level0 = 0,
    Level1 = 1,
    Level2 = 2,
    Level3 = 3,
    Level4 = 4
}
