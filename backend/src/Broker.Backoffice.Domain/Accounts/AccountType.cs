using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Accounts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccountType
{
    Individual = 0,
    Corporate = 1,
    Joint = 2,
    Trust = 3,
    IRA = 4
}
