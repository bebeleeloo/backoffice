using Broker.Backoffice.Domain.Common;
using Broker.Backoffice.Domain.Orders;

namespace Broker.Backoffice.Domain.Transactions;

public sealed class Transaction : AuditableEntity
{
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }

    public string TransactionNumber { get; set; } = string.Empty;
    public OrderCategory Category { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime TransactionDate { get; set; }

    public string? Comment { get; set; }
    public string? ExternalId { get; set; }
}
