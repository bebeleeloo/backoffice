using Broker.Auth.Domain.Common;

namespace Broker.Auth.Domain.Identity;

public sealed class UserPermissionOverride : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    public bool IsAllowed { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
