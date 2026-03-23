using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Broker.Gateway.Api.Persistence;

public sealed class GatewayDbContextFactory : IDesignTimeDbContextFactory<GatewayDbContext>
{
    public GatewayDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GatewayDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=BrokerBackoffice;Username=postgres;Password=postgres");
        return new GatewayDbContext(optionsBuilder.Options);
    }
}
