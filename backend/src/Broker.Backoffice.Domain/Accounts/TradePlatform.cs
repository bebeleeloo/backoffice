namespace Broker.Backoffice.Domain.Accounts;

public sealed class TradePlatform
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
