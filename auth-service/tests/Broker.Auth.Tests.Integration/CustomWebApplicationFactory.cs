using Broker.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace Broker.Auth.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Broker.Auth.Api.Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
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
                options.UseSqlServer(_msSqlContainer.GetConnectionString()));
        });

        builder.UseSetting("RateLimiting:LoginPermitLimit", "10000");
    }

    public async Task InitializeAsync() => await _msSqlContainer.StartAsync();
    async Task IAsyncLifetime.DisposeAsync() => await _msSqlContainer.DisposeAsync();
}
