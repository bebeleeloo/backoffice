using Broker.Backoffice.Domain.Clients;

namespace Broker.Backoffice.Domain.Accounts;

public sealed class AccountHolder
{
    public Guid AccountId { get; set; }
    public Account? Account { get; set; }

    public Guid ClientId { get; set; }
    public Client? Client { get; set; }

    public HolderRole Role { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime AddedAt { get; set; }
}
