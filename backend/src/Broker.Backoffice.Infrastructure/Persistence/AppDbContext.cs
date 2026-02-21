using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Accounts;
using Broker.Backoffice.Domain.Audit;
using Broker.Backoffice.Domain.Clients;
using Broker.Backoffice.Domain.Countries;
using Broker.Backoffice.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermissionOverride> UserPermissionOverrides => Set<UserPermissionOverride>();
    public DbSet<DataScope> DataScopes => Set<DataScope>();
    public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientAddress> ClientAddresses => Set<ClientAddress>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<InvestmentProfile> InvestmentProfiles => Set<InvestmentProfile>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountHolder> AccountHolders => Set<AccountHolder>();
    public DbSet<Clearer> Clearers => Set<Clearer>();
    public DbSet<TradePlatform> TradePlatforms => Set<TradePlatform>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
