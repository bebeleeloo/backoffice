using Broker.Backoffice.Domain.Accounts;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Domain.Instruments;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Broker.Backoffice.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration config, ILogger logger)
    {
        // Seed countries reference data
        await SeedCountries.SeedAsync(db);

        // Seed clearers
        var clearerData = new (string Name, string Description)[]
        {
            ("Apex Clearing", "Full-service clearing and custody solutions"),
            ("Pershing", "BNY Mellon subsidiary providing clearing services"),
            ("Interactive Brokers", "Electronic brokerage and clearing"),
            ("Hilltop Securities", "Clearing and settlement services"),
        };
        var existingClearerNames = (await db.Clearers.Select(c => c.Name).ToListAsync()).ToHashSet();
        foreach (var (name, description) in clearerData)
        {
            if (existingClearerNames.Contains(name)) continue;
            db.Clearers.Add(new Clearer
            {
                Id = Guid.NewGuid(), Name = name, Description = description, IsActive = true
            });
        }
        await db.SaveChangesAsync();

        // Seed trade platforms
        var tradePlatformData = new (string Name, string Description)[]
        {
            ("MetaTrader 5", "Multi-asset trading platform"),
            ("Sterling Trader", "Professional trading platform"),
            ("DAS Trader", "Direct access trading software"),
            ("Thinkorswim", "Advanced trading platform by TD Ameritrade"),
        };
        var existingPlatformNames = (await db.TradePlatforms.Select(t => t.Name).ToListAsync()).ToHashSet();
        foreach (var (name, description) in tradePlatformData)
        {
            if (existingPlatformNames.Contains(name)) continue;
            db.TradePlatforms.Add(new TradePlatform
            {
                Id = Guid.NewGuid(), Name = name, Description = description, IsActive = true
            });
        }
        await db.SaveChangesAsync();

        // Seed currencies
        var currencyData = new (string Code, string Name, string? Symbol)[]
        {
            ("USD", "US Dollar", "$"), ("EUR", "Euro", "\u20ac"), ("GBP", "British Pound", "\u00a3"),
            ("JPY", "Japanese Yen", "\u00a5"), ("CHF", "Swiss Franc", "CHF"), ("CAD", "Canadian Dollar", "C$"),
            ("AUD", "Australian Dollar", "A$"), ("HKD", "Hong Kong Dollar", "HK$"),
            ("SGD", "Singapore Dollar", "S$"), ("CNY", "Chinese Yuan", "\u00a5"),
            ("INR", "Indian Rupee", "\u20b9"), ("KRW", "South Korean Won", "\u20a9"),
            ("BRL", "Brazilian Real", "R$"), ("ZAR", "South African Rand", "R"),
            ("NZD", "New Zealand Dollar", "NZ$"),
        };
        var existingCurrencyCodes = (await db.Currencies.Select(c => c.Code).ToListAsync()).ToHashSet();
        foreach (var (code, name, symbol) in currencyData)
        {
            if (existingCurrencyCodes.Contains(code)) continue;
            db.Currencies.Add(new Currency
            {
                Id = Guid.NewGuid(), Code = code, Name = name, Symbol = symbol, IsActive = true
            });
        }
        await db.SaveChangesAsync();

        // Seed exchanges
        var countryMap = await db.Countries.Where(c => c.IsActive).ToDictionaryAsync(c => c.Iso2, c => c.Id);
        var exchangeData = new (string Code, string Name, string CountryIso2)[]
        {
            ("NYSE", "New York Stock Exchange", "US"),
            ("NASDAQ", "NASDAQ Stock Market", "US"),
            ("LSE", "London Stock Exchange", "GB"),
            ("TSE", "Tokyo Stock Exchange", "JP"),
            ("HKEX", "Hong Kong Exchanges", "HK"),
            ("Euronext", "Euronext", "NL"),
            ("SSE", "Shanghai Stock Exchange", "CN"),
            ("SZSE", "Shenzhen Stock Exchange", "CN"),
            ("BSE", "Bombay Stock Exchange", "IN"),
            ("NSE", "National Stock Exchange of India", "IN"),
            ("ASX", "Australian Securities Exchange", "AU"),
            ("TMX", "Toronto Stock Exchange", "CA"),
            ("JSE", "Johannesburg Stock Exchange", "ZA"),
            ("XETRA", "Deutsche Boerse Xetra", "DE"),
            ("SIX", "SIX Swiss Exchange", "CH"),
        };
        var existingExchangeCodes = (await db.Exchanges.Select(e => e.Code).ToListAsync()).ToHashSet();
        foreach (var (code, name, countryIso2) in exchangeData)
        {
            if (existingExchangeCodes.Contains(code)) continue;
            db.Exchanges.Add(new Exchange
            {
                Id = Guid.NewGuid(), Code = code, Name = name,
                CountryId = countryMap.GetValueOrDefault(countryIso2),
                IsActive = true
            });
        }
        await db.SaveChangesAsync();

        // Seed permissions
        var existingCodes = (await db.Permissions.Select(p => p.Code).ToListAsync()).ToHashSet();
        foreach (var (code, name, group) in Permissions.All)
        {
            if (existingCodes.Contains(code)) continue;
            db.Permissions.Add(new Permission
            {
                Id = Guid.NewGuid(), Code = code, Name = name,
                Group = group, CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        // Seed admin role
        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole is null)
        {
            adminRole = new Role
            {
                Id = Guid.NewGuid(), Name = "Admin",
                Description = "Full access", IsSystem = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Roles.Add(adminRole);
            await db.SaveChangesAsync();
        }

        // Ensure admin role has all permissions (add via RolePermissions DbSet directly)
        var allPerms = await db.Permissions.ToListAsync();
        var existingPermIds = (await db.RolePermissions
            .Where(rp => rp.RoleId == adminRole.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync()).ToHashSet();

        foreach (var perm in allPerms.Where(p => !existingPermIds.Contains(p.Id)))
        {
            db.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(), RoleId = adminRole.Id, PermissionId = perm.Id,
                CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        // Seed admin user
        if (!await db.Users.AnyAsync(u => u.Username == "admin"))
        {
            var password = config["ADMIN_PASSWORD"] ?? "Admin123!";
            var hasher = new PasswordHasher<User>();
            var admin = new User
            {
                Id = Guid.NewGuid(), Username = "admin",
                Email = "admin@broker.local", FullName = "System Administrator",
                IsActive = true, CreatedAt = DateTime.UtcNow
            };
            admin.PasswordHash = hasher.HashPassword(admin, password);
            db.Users.Add(admin);
            await db.SaveChangesAsync();

            db.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(), UserId = admin.Id, RoleId = adminRole.Id,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded admin user (username: admin)");
        }

        // Demo data: seeds 10 users + 100 clients for dev/demo environments.
        // Enabled automatically in Development, or manually via SEED_DEMO_DATA=true.
        // Never runs in Production even if SEED_DEMO_DATA=true.
        // Password for demo users: DEFAULT_DEMO_PASSWORD env var (default: ADMIN_PASSWORD).
        var env = config["ASPNETCORE_ENVIRONMENT"] ?? "";
        var isProduction = string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);
        var seedDemo = string.Equals(config["SEED_DEMO_DATA"], "true", StringComparison.OrdinalIgnoreCase);

        if (isProduction && seedDemo)
        {
            logger.LogWarning("SEED_DEMO_DATA=true is ignored in Production environment");
        }
        else if (!isProduction && (string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase) || seedDemo))
        {
            await SeedDemoData.SeedAsync(db, config, logger);
        }
    }
}
