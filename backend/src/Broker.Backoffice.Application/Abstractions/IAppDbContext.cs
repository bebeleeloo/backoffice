using Broker.Backoffice.Domain.Accounts;
using Broker.Backoffice.Domain.Audit;
using Broker.Backoffice.Domain.Clients;
using Broker.Backoffice.Domain.Countries;
using Broker.Backoffice.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Broker.Backoffice.Application.Abstractions;

public interface IAppDbContext
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
    DbSet<Client> Clients { get; }
    DbSet<ClientAddress> ClientAddresses { get; }
    DbSet<Country> Countries { get; }
    DbSet<InvestmentProfile> InvestmentProfiles { get; }
    DbSet<Account> Accounts { get; }
    DbSet<AccountHolder> AccountHolders { get; }
    DbSet<Clearer> Clearers { get; }
    DbSet<TradePlatform> TradePlatforms { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
