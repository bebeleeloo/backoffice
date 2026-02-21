using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Instruments;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InstrumentStatus
{
    Active = 0,
    Inactive = 1,
    Delisted = 2,
    Suspended = 3
}
