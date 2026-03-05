using Broker.Backoffice.Domain.Accounts;
using Broker.Backoffice.Domain.Instruments;
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

        // Demo data: seeds 100 clients, accounts, instruments, orders, transactions for dev/demo.
        var env = config["ASPNETCORE_ENVIRONMENT"] ?? "";
        var seedDemo = string.Equals(config["SEED_DEMO_DATA"], "true", StringComparison.OrdinalIgnoreCase);
        var isDevelopment = string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);

        if (seedDemo || isDevelopment)
        {
            await SeedDemoData.SeedAsync(db, config, logger);
        }
    }
}
