using Broker.Backoffice.Domain.Accounts;
using Broker.Backoffice.Domain.Common;

namespace Broker.Backoffice.Domain.Orders;

public sealed class Order : AuditableEntity
{
    public Guid AccountId { get; set; }
    public Account? Account { get; set; }

    public string OrderNumber { get; set; } = string.Empty;
    public OrderCategory Category { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime OrderDate { get; set; }

    public string? Comment { get; set; }
    public string? ExternalId { get; set; }
}
