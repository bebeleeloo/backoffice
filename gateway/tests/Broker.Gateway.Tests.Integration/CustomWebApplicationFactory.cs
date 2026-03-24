using Broker.Gateway.Api.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Broker.Gateway.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Broker.Gateway.Api.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _pgContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    private string _configDir = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Create temp config directory with YAML files
        _configDir = Path.Combine(Path.GetTempPath(), $"gateway-test-config-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_configDir);
        CopyConfigFiles(_configDir);

        builder.UseSetting("ConfigDir", _configDir);
        builder.UseSetting("ConnectionStrings:DefaultConnection", _pgContainer.GetConnectionString());
        builder.UseSetting("Jwt:Secret", "this-is-a-development-secret-key-min-32-chars!!");
        builder.UseSetting("Jwt:Issuer", "BrokerBackoffice");
        builder.UseSetting("Jwt:Audience", "BrokerBackoffice");
        builder.UseSetting("RateLimiting:ConfigPermitLimit", "10000");

        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<GatewayDbContext>));

            if (descriptor is not null)
                services.Remove(descriptor);

            // Add DbContext with Testcontainers connection string
            services.AddDbContext<GatewayDbContext>(options =>
                options.UseNpgsql(_pgContainer.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        await _pgContainer.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _pgContainer.DisposeAsync();

        // Clean up temp config directory
        if (Directory.Exists(_configDir))
        {
            try { Directory.Delete(_configDir, recursive: true); } catch { }
        }
    }

    private static void CopyConfigFiles(string targetDir)
    {
        // Find the gateway config directory relative to the test project
        var sourceDir = FindConfigDirectory();

        foreach (var file in Directory.GetFiles(sourceDir, "*.yaml"))
        {
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)));
        }
    }

    private static string FindConfigDirectory()
    {
        // Walk up from the test assembly location to find gateway/config
        var dir = AppContext.BaseDirectory;

        for (var i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(dir, "config");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "menu.yaml")))
                return candidate;

            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        throw new InvalidOperationException(
            $"Could not find gateway config directory. Started from: {AppContext.BaseDirectory}");
    }
}
