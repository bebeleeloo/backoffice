using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Accounts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HolderRole
{
    Owner = 0,
    Beneficiary = 1,
    Trustee = 2,
    PowerOfAttorney = 3,
    Custodian = 4,
    Authorized = 5
}
