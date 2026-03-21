using System.Text;
using Broker.Auth.Application.Abstractions;
using Broker.Auth.Domain.Identity;
using Broker.Auth.Infrastructure.Auth;
using Broker.Auth.Infrastructure.Persistence;
using Broker.Auth.Infrastructure.Persistence.ChangeTracking;
using Broker.Auth.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Broker.Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName)));

        services.AddScoped<IAuthDbContext>(sp => sp.GetRequiredService<AuthDbContext>());

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IAuditContext, AuditContext>();
        services.AddScoped<IChangeTrackingContext, ChangeTrackingContext>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<PasswordHasher<User>>();
        services.AddHttpContextAccessor();

        var jwtSecret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
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
            });

        services.AddHostedService<RefreshTokenCleanupService>();

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
