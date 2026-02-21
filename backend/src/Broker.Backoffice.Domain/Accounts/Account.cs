using Broker.Backoffice.Domain.Common;

namespace Broker.Backoffice.Domain.Accounts;

public sealed class Account : AuditableEntity
{
    public string Number { get; set; } = string.Empty;

    public Guid? ClearerId { get; set; }
    public Clearer? Clearer { get; set; }

    public Guid? TradePlatformId { get; set; }
    public TradePlatform? TradePlatform { get; set; }

    public AccountStatus Status { get; set; }
    public AccountType AccountType { get; set; }
    public MarginType MarginType { get; set; }
    public OptionLevel OptionLevel { get; set; }
    public Tariff Tariff { get; set; }

    public DateTime? OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public DeliveryType? DeliveryType { get; set; }

    public string? Comment { get; set; }
    public string? ExternalId { get; set; }

    public ICollection<AccountHolder> Holders { get; set; } = [];
}
