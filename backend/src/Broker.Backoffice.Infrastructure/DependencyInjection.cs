using System.Text;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Infrastructure.Auth;
using Broker.Backoffice.Infrastructure.Persistence;
using Broker.Backoffice.Infrastructure.Persistence.ChangeTracking;
using Broker.Backoffice.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Broker.Backoffice.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Services
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IAuditContext, AuditContext>();
        services.AddScoped<IChangeTrackingContext, ChangeTrackingContext>();
        services.AddHttpContextAccessor();

        // Auth Service client
        var authServiceUrl = configuration["AuthService:BaseUrl"] ?? "http://auth:8082";
        services.AddHttpClient<IAuthServiceClient, AuthServiceClient>(client =>
        {
            client.BaseAddress = new Uri(authServiceUrl);
        });
        services.AddHttpClient("AuthService", client =>
        {
            client.BaseAddress = new Uri(authServiceUrl);
        });

        // JWT Authentication
        var jwtSecret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured");

        services.AddMemoryCache();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "JwtOrBasic";
                options.DefaultChallengeScheme = "JwtOrBasic";
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            })
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
                BasicAuthenticationHandler.SchemeName, null)
            .AddPolicyScheme("JwtOrBasic", "JWT or Basic", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var auth = context.Request.Headers.Authorization.ToString();
                    return auth.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)
                        ? BasicAuthenticationHandler.SchemeName
                        : JwtBearerDefaults.AuthenticationScheme;
                };
            });

        // Authorization
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthorizationOptions>>();
            var defaultProvider = new DefaultAuthorizationPolicyProvider(options);
            return new PermissionPolicyProvider(defaultProvider);
        });

        return services;
    }
}
