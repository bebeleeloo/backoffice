using Broker.Backoffice.Domain.Common;

namespace Broker.Backoffice.Domain.Identity;

public sealed class User : AuditableEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<UserPermissionOverride> PermissionOverrides { get; set; } = [];
    public ICollection<UserRefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<DataScope> DataScopes { get; set; } = [];
}
