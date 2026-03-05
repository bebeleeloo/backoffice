using Broker.Backoffice.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace Broker.Backoffice.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Broker.Backoffice.Api.Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor is not null)
                services.Remove(descriptor);

            // Add DbContext with Testcontainers connection string
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(_msSqlContainer.GetConnectionString()));

            // Mock IAuthServiceClient (auth service is separate)
            var authClientDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(Broker.Backoffice.Application.Abstractions.IAuthServiceClient));
            if (authClientDescriptor is not null)
                services.Remove(authClientDescriptor);

            // Also remove HttpClient registrations for AuthServiceClient
            var httpClientFactoryDescriptors = services
                .Where(d => d.ServiceType == typeof(System.Net.Http.IHttpClientFactory))
                .ToList();

            services.AddSingleton<Broker.Backoffice.Application.Abstractions.IAuthServiceClient>(
                new TestAuthServiceClient());
        });

        // Disable rate limiting for integration tests
        builder.UseSetting("RateLimiting:LoginPermitLimit", "10000");
    }

    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _msSqlContainer.DisposeAsync();
    }
}

internal class TestAuthServiceClient : Broker.Backoffice.Application.Abstractions.IAuthServiceClient
{
    public Task<Broker.Backoffice.Application.Abstractions.UserStatsDto> GetUserStatsAsync(CancellationToken ct = default)
        => Task.FromResult(new Broker.Backoffice.Application.Abstractions.UserStatsDto(5, 4));
}
