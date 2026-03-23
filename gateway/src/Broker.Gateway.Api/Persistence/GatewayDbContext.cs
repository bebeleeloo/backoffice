using Broker.Gateway.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Broker.Gateway.Api.Persistence;

public sealed class GatewayDbContext(DbContextOptions<GatewayDbContext> options) : DbContext(options)
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("gateway");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GatewayDbContext).Assembly);
    }
}
