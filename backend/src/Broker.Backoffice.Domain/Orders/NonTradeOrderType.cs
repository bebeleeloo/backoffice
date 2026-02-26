using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Orders;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NonTradeOrderType
{
    Deposit = 0,
    Withdrawal = 1,
    Dividend = 2,
    CorporateAction = 3,
    Fee = 4,
    Interest = 5,
    Transfer = 6,
    Adjustment = 7
}
