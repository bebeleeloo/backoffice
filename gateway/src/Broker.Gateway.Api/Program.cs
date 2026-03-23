using System.Text;
using Broker.Gateway.Api.Filters;
using Broker.Gateway.Api.Middleware;
using Broker.Gateway.Api.Persistence;
using Broker.Gateway.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Yarp.ReverseProxy.Configuration;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, cfg) =>
        cfg.ReadFrom.Configuration(context.Configuration)
           .Enrich.FromLogContext()
           .Enrich.WithCorrelationId()
           .WriteTo.Console(outputTemplate:
               "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}"));

    // Config loader (YAML)
    builder.Services.AddSingleton<ConfigLoader>();
    builder.Services.AddSingleton<MenuService>();
    builder.Services.AddSingleton<EntityConfigService>();
    builder.Services.AddSingleton<ConfigDiffService>();

    // PostgreSQL (gateway schema)
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured");

    builder.Services.AddDbContext<GatewayDbContext>(options =>
        options.UseNpgsql(connectionString));

    // Audit services
    builder.Services.AddScoped<IAuditContext, AuditContext>();
    builder.Services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();
    builder.Services.AddScoped<AuditActionFilter>();

    // CORS (same pattern as backend/auth-service)
    var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            if (corsOrigins.Length > 0)
                policy.WithOrigins(corsOrigins).AllowAnyMethod().AllowAnyHeader();
            else if (builder.Environment.IsDevelopment())
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            else
                throw new InvalidOperationException("Cors:Origins must be configured in non-Development environments");
        });
    });

    // JWT authentication (same config as backend/auth-service)
    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret is not configured");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        // Dynamic permission policies (same pattern as backend)
        options.AddPolicy("settings.manage", policy =>
            policy.RequireClaim("permission", "settings.manage"));
    });

    // YARP reverse proxy — configured programmatically from upstreams.yaml
    builder.Services.AddReverseProxy();
    builder.Services.AddSingleton<IProxyConfigProvider, YamlProxyConfigProvider>();

    // Controllers (for ConfigController)
    builder.Services.AddControllers();

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Broker Gateway", Version = "v1" });
    });

    // Health checks
    builder.Services.AddHealthChecks()
        .AddCheck<Broker.Gateway.Api.HealthChecks.PostgresHealthCheck>(
            "postgres", tags: new[] { "ready" });

    // Response compression
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });

    var app = builder.Build();

    // Apply migrations
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<GatewayDbContext>();
        db.Database.Migrate();
    }

    // ForwardedHeaders (nginx → gateway)
    var forwardedHeadersOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                         | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
    };
    forwardedHeadersOptions.KnownProxies.Clear();
    forwardedHeadersOptions.KnownNetworks.Clear();
    app.UseForwardedHeaders(forwardedHeadersOptions);

    // Wire up YARP config reload when upstreams change
    var configLoader = app.Services.GetRequiredService<ConfigLoader>();
    var proxyConfigProvider = app.Services.GetRequiredService<IProxyConfigProvider>() as YamlProxyConfigProvider;
    if (proxyConfigProvider != null)
    {
        configLoader.OnUpstreamsChanged += proxyConfigProvider.Update;
    }

    app.UseResponseCompression();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Health endpoints
    app.MapHealthChecks("/health/live", new()
    {
        Predicate = _ => false // liveness = always 200
    });
    app.MapHealthChecks("/health/ready");

    // YARP reverse proxy
    app.MapReverseProxy();

    Log.Information("Gateway starting on {Urls}", builder.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:8090");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Gateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

namespace Broker.Gateway.Api { public partial class Program; }
