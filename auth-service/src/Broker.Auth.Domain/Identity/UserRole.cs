using Broker.Auth.Domain.Common;

namespace Broker.Auth.Domain.Identity;

public sealed class UserRole : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
