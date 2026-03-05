using Broker.Auth.Domain.Common;

namespace Broker.Auth.Domain.Identity;

public sealed class DataScope : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string ScopeType { get; set; } = string.Empty;
    public string ScopeValue { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
