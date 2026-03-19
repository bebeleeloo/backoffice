using Broker.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Broker.Auth.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Broker.Auth.Api.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _pgContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AuthDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<AuthDbContext>(options =>
                options.UseNpgsql(_pgContainer.GetConnectionString()));
        });

        builder.UseSetting("RateLimiting:LoginPermitLimit", "10000");
    }

    public async Task InitializeAsync()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        await _pgContainer.StartAsync();
    }
    async Task IAsyncLifetime.DisposeAsync() => await _pgContainer.DisposeAsync();
}
