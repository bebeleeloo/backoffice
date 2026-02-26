using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Orders;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    New = 0,
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3,
    InProgress = 4,
    PartiallyFilled = 5,
    Filled = 6,
    Completed = 7,
    Cancelled = 8,
    Failed = 9
}
