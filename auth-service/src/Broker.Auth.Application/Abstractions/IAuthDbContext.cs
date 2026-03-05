using Broker.Auth.Domain.Audit;
using Broker.Auth.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Broker.Auth.Application.Abstractions;

public interface IAuthDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserPermissionOverride> UserPermissionOverrides { get; }
    DbSet<DataScope> DataScopes { get; }
    DbSet<UserRefreshToken> UserRefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<EntityChange> EntityChanges { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
