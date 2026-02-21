namespace Broker.Backoffice.Domain.Accounts;

public sealed class Clearer
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
