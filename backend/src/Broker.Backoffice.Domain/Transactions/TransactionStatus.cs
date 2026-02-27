using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Transactions;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionStatus
{
    Pending = 0,
    Settled = 1,
    Failed = 2,
    Cancelled = 3
}
