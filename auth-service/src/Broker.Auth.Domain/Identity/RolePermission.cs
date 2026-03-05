using Broker.Auth.Domain.Common;

namespace Broker.Auth.Domain.Identity;

public sealed class RolePermission : Entity
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
