using Broker.Backoffice.Domain.Accounts;
using Broker.Backoffice.Domain.Clients;
using Broker.Backoffice.Domain.Countries;
using Broker.Backoffice.Domain.Identity;
using Broker.Backoffice.Domain.Instruments;
using Broker.Backoffice.Domain.Orders;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Broker.Backoffice.Infrastructure.Persistence;

public static class SeedDemoData
{
    private const int ClientCount = 100;

    public static async Task SeedAsync(AppDbContext db, IConfiguration config, ILogger logger)
    {
        var password = config["DEFAULT_DEMO_PASSWORD"] ?? config["ADMIN_PASSWORD"] ?? "Admin123!";

        await using var tx = await db.Database.BeginTransactionAsync();

        var usersCreated = await SeedUsersAsync(db, password, logger);
        var clientsCreated = await SeedClientsAsync(db, logger);
        var accountsCreated = await SeedAccountsAsync(db, logger);
        var instrumentsCreated = await SeedInstrumentsAsync(db, logger);
        var ordersCreated = await SeedOrdersAsync(db, logger);

        await tx.CommitAsync();

        if (usersCreated + clientsCreated + accountsCreated + instrumentsCreated + ordersCreated > 0)
            logger.LogInformation("Demo seed complete: {Users} users, {Clients} clients, {Accounts} accounts, {Instruments} instruments, {Orders} orders created",
                usersCreated, clientsCreated, accountsCreated, instrumentsCreated, ordersCreated);
        else
            logger.LogInformation("Demo seed: all data already present, 0 created");
    }

    // ── Users ────────────────────────────────────────────────────────

    private static readonly (string Username, string Email, string FullName, string RoleName)[] DemoUsers =
    [
        ("jdoe",       "john.doe@broker.local",       "John Doe",           "Manager"),
        ("asmith",     "alice.smith@broker.local",     "Alice Smith",        "Manager"),
        ("bwilson",    "bob.wilson@broker.local",      "Bob Wilson",         "Manager"),
        ("cjohnson",   "carol.johnson@broker.local",   "Carol Johnson",      "Viewer"),
        ("dlee",       "david.lee@broker.local",       "David Lee",          "Viewer"),
        ("ebrown",     "eva.brown@broker.local",       "Eva Brown",          "Viewer"),
        ("fgarcia",    "frank.garcia@broker.local",    "Frank Garcia",       "Operator"),
        ("gmartinez",  "grace.martinez@broker.local",  "Grace Martinez",     "Operator"),
        ("hchen",      "henry.chen@broker.local",      "Henry Chen",         "Operator"),
        ("itaylor",    "iris.taylor@broker.local",     "Iris Taylor",        "Operator"),
    ];

    private static readonly Dictionary<string, (string Desc, string[] Perms)> RoleDefinitions = new()
    {
        ["Manager"] = ("Manage clients, accounts, instruments, orders, and view everything", new[]
        {
            Permissions.UsersRead, Permissions.RolesRead, Permissions.PermissionsRead,
            Permissions.AuditRead,
            Permissions.ClientsRead, Permissions.ClientsCreate, Permissions.ClientsUpdate, Permissions.ClientsDelete,
            Permissions.AccountsRead, Permissions.AccountsCreate, Permissions.AccountsUpdate, Permissions.AccountsDelete,
            Permissions.InstrumentsRead, Permissions.InstrumentsCreate, Permissions.InstrumentsUpdate, Permissions.InstrumentsDelete,
            Permissions.OrdersRead, Permissions.OrdersCreate, Permissions.OrdersUpdate, Permissions.OrdersDelete,
        }),
        ["Viewer"] = ("Read-only access", new[]
        {
            Permissions.UsersRead, Permissions.RolesRead, Permissions.PermissionsRead,
            Permissions.AuditRead, Permissions.ClientsRead, Permissions.AccountsRead, Permissions.InstrumentsRead,
            Permissions.OrdersRead,
        }),
        ["Operator"] = ("Client, account, instrument, and order operations", new[]
        {
            Permissions.ClientsRead, Permissions.ClientsCreate, Permissions.ClientsUpdate,
            Permissions.AccountsRead, Permissions.AccountsCreate, Permissions.AccountsUpdate,
            Permissions.InstrumentsRead, Permissions.InstrumentsCreate, Permissions.InstrumentsUpdate,
            Permissions.OrdersRead, Permissions.OrdersCreate, Permissions.OrdersUpdate,
        }),
    };

    private static async Task<int> SeedUsersAsync(AppDbContext db, string password, ILogger logger)
    {
        var existingUsernames = (await db.Users.Select(u => u.Username).ToListAsync()).ToHashSet();
        var allPermissions = await db.Permissions.ToDictionaryAsync(p => p.Code);

        // Ensure roles exist
        var roles = await db.Roles.ToDictionaryAsync(r => r.Name);
        foreach (var (roleName, (desc, permCodes)) in RoleDefinitions)
        {
            if (!roles.ContainsKey(roleName))
            {
                var role = new Role
                {
                    Id = Guid.NewGuid(), Name = roleName,
                    Description = desc, IsSystem = false, CreatedAt = DateTime.UtcNow,
                };
                db.Roles.Add(role);
                roles[roleName] = role;
            }
        }
        await db.SaveChangesAsync();

        // Ensure role permissions
        foreach (var (roleName, (_, permCodes)) in RoleDefinitions)
        {
            var role = roles[roleName];
            var existingPermIds = (await db.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync()).ToHashSet();

            foreach (var code in permCodes)
            {
                if (allPermissions.TryGetValue(code, out var perm) && !existingPermIds.Contains(perm.Id))
                {
                    db.RolePermissions.Add(new RolePermission
                    {
                        Id = Guid.NewGuid(), RoleId = role.Id, PermissionId = perm.Id,
                        CreatedAt = DateTime.UtcNow,
                    });
                }
            }
        }
        await db.SaveChangesAsync();

        // Create users (single batch)
        var hasher = new PasswordHasher<User>();
        var created = 0;
        foreach (var (username, email, fullName, roleName) in DemoUsers)
        {
            if (existingUsernames.Contains(username)) continue;

            var user = new User
            {
                Id = Guid.NewGuid(), Username = username,
                Email = email, FullName = fullName,
                IsActive = true, CreatedAt = DateTime.UtcNow,
            };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);

            db.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(), UserId = user.Id, RoleId = roles[roleName].Id,
                CreatedAt = DateTime.UtcNow,
            });
            created++;
        }
        if (created > 0) await db.SaveChangesAsync();

        if (created > 0)
            logger.LogInformation("Demo seed: created {Count} users with roles (Manager/Viewer/Operator)", created);

        return created;
    }

    // ── Clients ──────────────────────────────────────────────────────

    private static readonly string[] FirstNames =
        ["James", "Mary", "Robert", "Patricia", "Michael", "Jennifer", "William", "Linda",
         "David", "Elizabeth", "Richard", "Barbara", "Joseph", "Susan", "Thomas", "Jessica",
         "Charles", "Sarah", "Christopher", "Karen", "Daniel", "Lisa", "Matthew", "Nancy",
         "Anthony", "Betty", "Mark", "Margaret", "Donald", "Sandra"];

    private static readonly string[] LastNames =
        ["Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
         "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson",
         "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson",
         "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson"];

    private static readonly string[] CompanyNames =
        ["Acme Corp", "Globex Industries", "Initech Solutions", "Vortex Holdings", "Apex Financial",
         "Pinnacle Group", "Summit Capital", "Horizon Ventures", "Atlas Partners", "Zenith Corp",
         "Meridian LLC", "Nexus Trading", "Vanguard Systems", "Eclipse Enterprises", "Quantum Dynamics"];

    private static readonly string[] Cities =
        ["New York", "London", "Tokyo", "Berlin", "Paris", "Sydney", "Toronto", "Singapore",
         "Dubai", "Zurich", "Hong Kong", "Amsterdam", "Seoul", "Vienna", "Stockholm"];

    private static readonly string[] Streets =
        ["Main St", "Oak Ave", "Pine Rd", "Maple Dr", "Cedar Ln", "Elm St", "Park Ave",
         "Broadway", "Market St", "High St", "Victoria Rd", "King St", "Queen Blvd", "River Rd"];

    private static readonly string[] CountryIso2Codes =
        ["US", "GB", "DE", "FR", "JP", "AU", "CA", "SG", "AE", "CH",
         "NL", "KR", "AT", "SE", "NO", "DK", "IE", "NZ", "IL", "IT"];

    private static async Task<int> SeedClientsAsync(AppDbContext db, ILogger logger)
    {
        var existingEmails = (await db.Clients.Select(c => c.Email).ToListAsync()).ToHashSet();

        // Pre-load country IDs by ISO2
        var countryMap = await db.Countries
            .Where(c => c.IsActive)
            .ToDictionaryAsync(c => c.Iso2, c => c.Id);

        var countryIds = CountryIso2Codes
            .Where(iso => countryMap.ContainsKey(iso))
            .Select(iso => countryMap[iso])
            .ToArray();

        if (countryIds.Length == 0)
        {
            logger.LogWarning("Demo seed: no countries found, skipping client seeding");
            return 0;
        }

        var created = 0;
        for (var i = 0; i < ClientCount; i++)
        {
            // Use per-client deterministic RNG so skipping existing clients doesn't shift state
            var clientRng = new Random(42 * 1000 + i);
            var isCorporate = i % 5 == 4; // ~20% corporate
            // Email is index-based (not random-dependent) for stable idempotency
            var email = isCorporate
                ? $"contact-{i:D3}@company{i:D3}.demo"
                : $"client-{i:D3}@demo.local";

            if (existingEmails.Contains(email)) continue;

            var r = clientRng; // shorthand
            var residenceCountryId = countryIds[r.Next(countryIds.Length)];
            var citizenshipCountryId = r.Next(3) == 0 ? countryIds[r.Next(countryIds.Length)] : (Guid?)null;

            var client = new Client
            {
                Id = Guid.NewGuid(),
                ClientType = isCorporate ? ClientType.Corporate : ClientType.Individual,
                ExternalId = i % 3 == 0 ? $"EXT-{i:D5}" : null,
                Status = PickWeighted(r, (ClientStatus.Active, 70), (ClientStatus.PendingKyc, 20), (ClientStatus.Blocked, 10)),
                Email = email,
                Phone = r.Next(2) == 0 ? $"+1-555-{r.Next(100, 999)}-{r.Next(1000, 9999)}" : null,
                PreferredLanguage = PickRandom(r, "en", "de", "fr", "ja", null),
                TimeZone = PickRandom(r, "America/New_York", "Europe/London", "Asia/Tokyo", "Europe/Berlin", null),
                ResidenceCountryId = residenceCountryId,
                CitizenshipCountryId = citizenshipCountryId,
                PepStatus = r.Next(20) == 0, // 5%
                RiskLevel = PickRandom<RiskLevel?>(r, RiskLevel.Low, RiskLevel.Medium, RiskLevel.High, null),
                KycStatus = PickWeighted(r, (KycStatus.Approved, 50), (KycStatus.InProgress, 20), (KycStatus.NotStarted, 20), (KycStatus.Rejected, 10)),
                KycReviewedAtUtc = r.Next(2) == 0 ? DateTime.UtcNow.AddDays(-r.Next(1, 365)) : null,
                CreatedAt = DateTime.UtcNow.AddDays(-r.Next(1, 730)),
            };

            if (isCorporate)
            {
                client.CompanyName = CompanyNames[i / 5 % CompanyNames.Length] + (i > 70 ? $" {i}" : "");
                client.RegistrationNumber = $"REG-{r.Next(100000, 999999)}";
                client.TaxId = r.Next(2) == 0 ? $"TAX-{r.Next(10000000, 99999999)}" : null;
            }
            else
            {
                client.FirstName = FirstNames[r.Next(FirstNames.Length)];
                client.LastName = LastNames[r.Next(LastNames.Length)];
                client.MiddleName = r.Next(3) == 0 ? FirstNames[r.Next(FirstNames.Length)] : null;
                client.DateOfBirth = r.Next(2) == 0
                    ? new DateOnly(r.Next(1950, 2003), r.Next(1, 13), r.Next(1, 28))
                    : null;
                client.Gender = PickRandom<Gender?>(r, Gender.Male, Gender.Female, Gender.Other, null);
                client.MaritalStatus = PickRandom<MaritalStatus?>(r,
                    MaritalStatus.Single, MaritalStatus.Married, MaritalStatus.Divorced, null);
                client.Education = PickRandom<Education?>(r,
                    Education.HighSchool, Education.Bachelor, Education.Master, Education.PhD, null);

                // Identity docs — fill for ~60% of individuals
                if (r.Next(5) < 3)
                    client.Ssn = $"{r.Next(100, 999)}-{r.Next(10, 99)}-{r.Next(1000, 9999)}";
                if (r.Next(3) == 0)
                    client.PassportNumber = $"P{r.Next(10000000, 99999999)}";
                if (r.Next(4) == 0)
                    client.DriverLicenseNumber = $"DL-{r.Next(100000, 999999)}";
            }

            db.Clients.Add(client);

            // 1-3 addresses per client
            var addressCount = r.Next(1, 4);
            var addressTypes = new[] { AddressType.Legal, AddressType.Mailing, AddressType.Working };
            for (var a = 0; a < addressCount; a++)
            {
                db.ClientAddresses.Add(new ClientAddress
                {
                    Id = Guid.NewGuid(),
                    ClientId = client.Id,
                    Type = addressTypes[a],
                    Line1 = $"{r.Next(1, 9999)} {Streets[r.Next(Streets.Length)]}",
                    Line2 = r.Next(3) == 0 ? $"Suite {r.Next(1, 500)}" : null,
                    City = Cities[r.Next(Cities.Length)],
                    State = r.Next(2) == 0 ? $"State-{r.Next(1, 50)}" : null,
                    PostalCode = $"{r.Next(10000, 99999)}",
                    CountryId = countryIds[r.Next(countryIds.Length)],
                });
            }

            // Investment profile for ~60% of clients
            if (r.Next(5) < 3)
            {
                db.InvestmentProfiles.Add(new InvestmentProfile
                {
                    Id = Guid.NewGuid(),
                    ClientId = client.Id,
                    Objective = PickRandom<InvestmentObjective?>(r,
                        InvestmentObjective.Growth, InvestmentObjective.Income,
                        InvestmentObjective.Preservation, InvestmentObjective.Speculation),
                    RiskTolerance = PickRandom<InvestmentRiskTolerance?>(r,
                        InvestmentRiskTolerance.Low, InvestmentRiskTolerance.Medium, InvestmentRiskTolerance.High),
                    LiquidityNeeds = PickRandom<LiquidityNeeds?>(r,
                        LiquidityNeeds.Low, LiquidityNeeds.Medium, LiquidityNeeds.High, null),
                    TimeHorizon = PickRandom<InvestmentTimeHorizon?>(r,
                        InvestmentTimeHorizon.Short, InvestmentTimeHorizon.Medium, InvestmentTimeHorizon.Long, null),
                    Knowledge = PickRandom<InvestmentKnowledge?>(r,
                        InvestmentKnowledge.None, InvestmentKnowledge.Basic,
                        InvestmentKnowledge.Good, InvestmentKnowledge.Advanced, null),
                    Experience = PickRandom<InvestmentExperience?>(r,
                        InvestmentExperience.None, InvestmentExperience.LessThan1Year,
                        InvestmentExperience.OneToThreeYears, InvestmentExperience.ThreeToFiveYears, null),
                    Notes = r.Next(3) == 0 ? "Demo seeded investment profile" : null,
                });
            }

            created++;

            // Flush in batches of 25 to avoid large change tracker
            if (created % 25 == 0)
                await db.SaveChangesAsync();
        }

        if (created > 0)
        {
            await db.SaveChangesAsync(); // flush remainder
            logger.LogInformation("Demo seed: created {Count} clients with addresses and investment profiles", created);
        }

        return created;
    }

    // ── Accounts ──────────────────────────────────────────────────────

    private const int AccountCount = 150;

    private static async Task<int> SeedAccountsAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Accounts.AnyAsync())
            return 0;

        var clearerIds = await db.Clearers.Where(c => c.IsActive).Select(c => c.Id).ToArrayAsync();
        var platformIds = await db.TradePlatforms.Where(t => t.IsActive).Select(t => t.Id).ToArrayAsync();
        var clientIds = await db.Clients.OrderBy(c => c.CreatedAt).Select(c => c.Id).ToArrayAsync();

        if (clearerIds.Length == 0 || platformIds.Length == 0 || clientIds.Length == 0)
        {
            logger.LogWarning("Demo seed: skipping accounts — no clearers, platforms, or clients found");
            return 0;
        }

        var accountIds = new List<Guid>();

        for (var i = 0; i < AccountCount; i++)
        {
            var rng = new Random(77 * 1000 + i);
            var accountId = Guid.NewGuid();
            accountIds.Add(accountId);

            var account = new Account
            {
                Id = accountId,
                Number = $"ACC-{i + 1:D5}",
                ClearerId = clearerIds[rng.Next(clearerIds.Length)],
                TradePlatformId = rng.Next(3) == 0 ? null : platformIds[rng.Next(platformIds.Length)],
                Status = PickWeighted(rng,
                    (AccountStatus.Active, 60),
                    (AccountStatus.Blocked, 10),
                    (AccountStatus.Closed, 15),
                    (AccountStatus.Suspended, 15)),
                AccountType = PickRandom(rng,
                    AccountType.Individual, AccountType.Corporate, AccountType.Joint,
                    AccountType.Trust, AccountType.IRA),
                MarginType = PickRandom(rng, MarginType.Cash, MarginType.MarginX1, MarginType.MarginX2, MarginType.MarginX4, MarginType.DayTrader),
                OptionLevel = PickRandom(rng,
                    OptionLevel.Level0, OptionLevel.Level1, OptionLevel.Level2,
                    OptionLevel.Level3, OptionLevel.Level4),
                Tariff = PickWeighted(rng,
                    (Tariff.Basic, 30), (Tariff.Standard, 40),
                    (Tariff.Premium, 20), (Tariff.VIP, 10)),
                DeliveryType = rng.Next(3) == 0 ? null : PickRandom(rng, DeliveryType.Paper, DeliveryType.Electronic),
                OpenedAt = DateTime.UtcNow.AddDays(-rng.Next(30, 730)),
                ClosedAt = rng.Next(8) == 0 ? DateTime.UtcNow.AddDays(-rng.Next(1, 30)) : null,
                Comment = rng.Next(4) == 0 ? "Demo seeded account" : null,
                ExternalId = rng.Next(3) == 0 ? $"EXT-ACC-{rng.Next(10000, 99999)}" : null,
                CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(30, 730)),
                CreatedBy = "seed",
            };

            db.Accounts.Add(account);

            if (i % 50 == 49)
                await db.SaveChangesAsync();
        }

        await db.SaveChangesAsync();

        // Seed AccountHolders: assign 1-3 clients to each account
        var holderCount = 0;
        var clientsWithAccounts = new HashSet<Guid>();

        for (var i = 0; i < accountIds.Count; i++)
        {
            var rng = new Random(88 * 1000 + i);
            var numHolders = rng.Next(1, 4); // 1 to 3
            var usedClientIndices = new HashSet<int>();

            for (var h = 0; h < numHolders; h++)
            {
                int clientIdx;
                do { clientIdx = rng.Next(clientIds.Length); }
                while (!usedClientIndices.Add(clientIdx));

                db.AccountHolders.Add(new AccountHolder
                {
                    AccountId = accountIds[i],
                    ClientId = clientIds[clientIdx],
                    Role = h == 0
                        ? HolderRole.Owner
                        : PickRandom(rng, HolderRole.Beneficiary, HolderRole.Trustee,
                            HolderRole.PowerOfAttorney, HolderRole.Custodian, HolderRole.Authorized),
                    IsPrimary = h == 0,
                    AddedAt = DateTime.UtcNow.AddDays(-rng.Next(1, 365)),
                });
                clientsWithAccounts.Add(clientIds[clientIdx]);
                holderCount++;
            }

            if (i % 20 == 19)
                await db.SaveChangesAsync();
        }

        // Ensure every client has at least one account
        var orphanRng = new Random(99_000);
        foreach (var clientId in clientIds)
        {
            if (clientsWithAccounts.Contains(clientId))
                continue;

            var accountIdx = orphanRng.Next(accountIds.Count);
            db.AccountHolders.Add(new AccountHolder
            {
                AccountId = accountIds[accountIdx],
                ClientId = clientId,
                Role = HolderRole.Owner,
                IsPrimary = false,
                AddedAt = DateTime.UtcNow.AddDays(-orphanRng.Next(1, 365)),
            });
            clientsWithAccounts.Add(clientId);
            holderCount++;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Demo seed: created {Accounts} accounts with {Holders} holders (all {Clients} clients linked)",
            accountIds.Count, holderCount, clientsWithAccounts.Count);

        return accountIds.Count;
    }

    // ── Instruments ────────────────────────────────────────────────────

    private static readonly (string Symbol, string Name, string Exchange, string Currency, string Country, Sector Sector)[] StockData =
    [
        ("AAPL", "Apple Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("MSFT", "Microsoft Corporation", "NASDAQ", "USD", "US", Sector.Technology),
        ("GOOGL", "Alphabet Inc.", "NASDAQ", "USD", "US", Sector.Communication),
        ("AMZN", "Amazon.com Inc.", "NASDAQ", "USD", "US", Sector.ConsumerDiscretionary),
        ("TSLA", "Tesla Inc.", "NASDAQ", "USD", "US", Sector.ConsumerDiscretionary),
        ("META", "Meta Platforms Inc.", "NASDAQ", "USD", "US", Sector.Communication),
        ("NVDA", "NVIDIA Corporation", "NASDAQ", "USD", "US", Sector.Technology),
        ("BRK.B", "Berkshire Hathaway Inc.", "NYSE", "USD", "US", Sector.Finance),
        ("JPM", "JPMorgan Chase & Co.", "NYSE", "USD", "US", Sector.Finance),
        ("V", "Visa Inc.", "NYSE", "USD", "US", Sector.Finance),
        ("JNJ", "Johnson & Johnson", "NYSE", "USD", "US", Sector.Healthcare),
        ("UNH", "UnitedHealth Group Inc.", "NYSE", "USD", "US", Sector.Healthcare),
        ("PG", "Procter & Gamble Co.", "NYSE", "USD", "US", Sector.ConsumerStaples),
        ("MA", "Mastercard Inc.", "NYSE", "USD", "US", Sector.Finance),
        ("HD", "The Home Depot Inc.", "NYSE", "USD", "US", Sector.ConsumerDiscretionary),
        ("CVX", "Chevron Corporation", "NYSE", "USD", "US", Sector.Energy),
        ("MRK", "Merck & Co. Inc.", "NYSE", "USD", "US", Sector.Healthcare),
        ("ABBV", "AbbVie Inc.", "NYSE", "USD", "US", Sector.Healthcare),
        ("PEP", "PepsiCo Inc.", "NASDAQ", "USD", "US", Sector.ConsumerStaples),
        ("KO", "The Coca-Cola Co.", "NYSE", "USD", "US", Sector.ConsumerStaples),
        ("AVGO", "Broadcom Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("COST", "Costco Wholesale Corp.", "NASDAQ", "USD", "US", Sector.ConsumerStaples),
        ("TMO", "Thermo Fisher Scientific", "NYSE", "USD", "US", Sector.Healthcare),
        ("WMT", "Walmart Inc.", "NYSE", "USD", "US", Sector.ConsumerStaples),
        ("ADBE", "Adobe Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("CRM", "Salesforce Inc.", "NYSE", "USD", "US", Sector.Technology),
        ("CSCO", "Cisco Systems Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("ACN", "Accenture plc", "NYSE", "USD", "US", Sector.Technology),
        ("NFLX", "Netflix Inc.", "NASDAQ", "USD", "US", Sector.Communication),
        ("AMD", "Advanced Micro Devices", "NASDAQ", "USD", "US", Sector.Technology),
        ("INTC", "Intel Corporation", "NASDAQ", "USD", "US", Sector.Technology),
        ("QCOM", "Qualcomm Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("TXN", "Texas Instruments Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("ORCL", "Oracle Corporation", "NYSE", "USD", "US", Sector.Technology),
        ("IBM", "International Business Machines", "NYSE", "USD", "US", Sector.Technology),
        ("NKE", "Nike Inc.", "NYSE", "USD", "US", Sector.ConsumerDiscretionary),
        ("DIS", "The Walt Disney Co.", "NYSE", "USD", "US", Sector.Communication),
        ("BA", "The Boeing Company", "NYSE", "USD", "US", Sector.Industrials),
        ("CAT", "Caterpillar Inc.", "NYSE", "USD", "US", Sector.Industrials),
        ("GS", "Goldman Sachs Group", "NYSE", "USD", "US", Sector.Finance),
        ("XOM", "Exxon Mobil Corporation", "NYSE", "USD", "US", Sector.Energy),
        ("LLY", "Eli Lilly and Company", "NYSE", "USD", "US", Sector.Healthcare),
        ("PFE", "Pfizer Inc.", "NYSE", "USD", "US", Sector.Healthcare),
        ("BMY", "Bristol-Myers Squibb", "NYSE", "USD", "US", Sector.Healthcare),
        ("AMGN", "Amgen Inc.", "NASDAQ", "USD", "US", Sector.Healthcare),
        ("GE", "General Electric Co.", "NYSE", "USD", "US", Sector.Industrials),
        ("MMM", "3M Company", "NYSE", "USD", "US", Sector.Industrials),
        ("HON", "Honeywell International", "NASDAQ", "USD", "US", Sector.Industrials),
        ("UPS", "United Parcel Service", "NYSE", "USD", "US", Sector.Industrials),
        ("RTX", "RTX Corporation", "NYSE", "USD", "US", Sector.Industrials),
        ("NEE", "NextEra Energy Inc.", "NYSE", "USD", "US", Sector.Utilities),
        ("DUK", "Duke Energy Corporation", "NYSE", "USD", "US", Sector.Utilities),
        ("SO", "Southern Company", "NYSE", "USD", "US", Sector.Utilities),
        ("AEP", "American Electric Power", "NASDAQ", "USD", "US", Sector.Utilities),
        ("SHW", "Sherwin-Williams Co.", "NYSE", "USD", "US", Sector.Materials),
        ("APD", "Air Products & Chemicals", "NYSE", "USD", "US", Sector.Materials),
        ("ECL", "Ecolab Inc.", "NYSE", "USD", "US", Sector.Materials),
        ("FCX", "Freeport-McMoRan Inc.", "NYSE", "USD", "US", Sector.Materials),
        ("AMT", "American Tower Corp.", "NYSE", "USD", "US", Sector.RealEstate),
        ("PLD", "Prologis Inc.", "NYSE", "USD", "US", Sector.RealEstate),
        // International stocks
        ("SHEL", "Shell plc", "LSE", "GBP", "GB", Sector.Energy),
        ("AZN", "AstraZeneca plc", "LSE", "GBP", "GB", Sector.Healthcare),
        ("HSBA", "HSBC Holdings plc", "LSE", "GBP", "GB", Sector.Finance),
        ("BP", "BP plc", "LSE", "GBP", "GB", Sector.Energy),
        ("GSK", "GSK plc", "LSE", "GBP", "GB", Sector.Healthcare),
        ("ULVR", "Unilever plc", "LSE", "GBP", "GB", Sector.ConsumerStaples),
        ("RIO", "Rio Tinto Group", "LSE", "GBP", "GB", Sector.Materials),
        ("7203", "Toyota Motor Corp.", "TSE", "JPY", "JP", Sector.ConsumerDiscretionary),
        ("6758", "Sony Group Corp.", "TSE", "JPY", "JP", Sector.Technology),
        ("6861", "Keyence Corporation", "TSE", "JPY", "JP", Sector.Technology),
        ("9984", "SoftBank Group Corp.", "TSE", "JPY", "JP", Sector.Communication),
        ("SAP", "SAP SE", "XETRA", "EUR", "DE", Sector.Technology),
        ("SIE", "Siemens AG", "XETRA", "EUR", "DE", Sector.Industrials),
        ("ALV", "Allianz SE", "XETRA", "EUR", "DE", Sector.Finance),
        ("BAS", "BASF SE", "XETRA", "EUR", "DE", Sector.Materials),
        ("MC", "LVMH Moet Hennessy", "Euronext", "EUR", "FR", Sector.ConsumerDiscretionary),
        ("OR", "L'Oreal SA", "Euronext", "EUR", "FR", Sector.ConsumerStaples),
        ("TTE", "TotalEnergies SE", "Euronext", "EUR", "FR", Sector.Energy),
        ("SAN", "Sanofi SA", "Euronext", "EUR", "FR", Sector.Healthcare),
        ("NESN", "Nestle SA", "SIX", "CHF", "CH", Sector.ConsumerStaples),
        ("ROG", "Roche Holding AG", "SIX", "CHF", "CH", Sector.Healthcare),
        ("NOVN", "Novartis AG", "SIX", "CHF", "CH", Sector.Healthcare),
        ("9988", "Alibaba Group", "HKEX", "HKD", "HK", Sector.Technology),
        ("700", "Tencent Holdings", "HKEX", "HKD", "HK", Sector.Technology),
        ("1299", "AIA Group Limited", "HKEX", "HKD", "HK", Sector.Finance),
        ("RY", "Royal Bank of Canada", "TMX", "CAD", "CA", Sector.Finance),
        ("TD", "Toronto-Dominion Bank", "TMX", "CAD", "CA", Sector.Finance),
        ("SHOP", "Shopify Inc.", "TMX", "CAD", "CA", Sector.Technology),
        ("BHP", "BHP Group Limited", "ASX", "AUD", "AU", Sector.Materials),
        ("CBA", "Commonwealth Bank", "ASX", "AUD", "AU", Sector.Finance),
        ("CSL", "CSL Limited", "ASX", "AUD", "AU", Sector.Healthcare),
        ("RELIANCE", "Reliance Industries", "NSE", "INR", "IN", Sector.Energy),
        ("TCS", "Tata Consultancy", "NSE", "INR", "IN", Sector.Technology),
        ("INFY", "Infosys Limited", "NSE", "INR", "IN", Sector.Technology),
        ("005930", "Samsung Electronics", "BSE", "KRW", "KR", Sector.Technology),
        ("NPN", "Naspers Limited", "JSE", "ZAR", "ZA", Sector.Communication),
        ("600519", "Kweichow Moutai", "SSE", "CNY", "CN", Sector.ConsumerStaples),
        ("601318", "Ping An Insurance", "SSE", "CNY", "CN", Sector.Finance),
        ("000858", "Wuliangye Yibin", "SZSE", "CNY", "CN", Sector.ConsumerStaples),
        // Additional US stocks to reach ~100
        ("PYPL", "PayPal Holdings Inc.", "NASDAQ", "USD", "US", Sector.Finance),
        ("SQ", "Block Inc.", "NYSE", "USD", "US", Sector.Finance),
        ("UBER", "Uber Technologies", "NYSE", "USD", "US", Sector.Technology),
        ("ABNB", "Airbnb Inc.", "NASDAQ", "USD", "US", Sector.ConsumerDiscretionary),
        ("SNOW", "Snowflake Inc.", "NYSE", "USD", "US", Sector.Technology),
        ("PLTR", "Palantir Technologies", "NYSE", "USD", "US", Sector.Technology),
        ("RIVN", "Rivian Automotive", "NASDAQ", "USD", "US", Sector.ConsumerDiscretionary),
        ("COIN", "Coinbase Global", "NASDAQ", "USD", "US", Sector.Finance),
        ("MRNA", "Moderna Inc.", "NASDAQ", "USD", "US", Sector.Healthcare),
        ("ZM", "Zoom Video Comms", "NASDAQ", "USD", "US", Sector.Technology),
        ("DDOG", "Datadog Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("NET", "Cloudflare Inc.", "NYSE", "USD", "US", Sector.Technology),
        ("CRWD", "CrowdStrike Holdings", "NASDAQ", "USD", "US", Sector.Technology),
        ("PANW", "Palo Alto Networks", "NASDAQ", "USD", "US", Sector.Technology),
        ("SPOT", "Spotify Technology", "NYSE", "USD", "US", Sector.Communication),
        ("RBLX", "Roblox Corporation", "NYSE", "USD", "US", Sector.Communication),
        ("SNAP", "Snap Inc.", "NYSE", "USD", "US", Sector.Communication),
        ("PINS", "Pinterest Inc.", "NYSE", "USD", "US", Sector.Communication),
        ("ROKU", "Roku Inc.", "NASDAQ", "USD", "US", Sector.Communication),
        ("SLB", "Schlumberger Limited", "NYSE", "USD", "US", Sector.Energy),
        // ── Additional US stocks (~200 more) ──
        ("LRCX", "Lam Research Corp.", "NASDAQ", "USD", "US", Sector.Technology),
        ("KLAC", "KLA Corporation", "NASDAQ", "USD", "US", Sector.Technology),
        ("MCHP", "Microchip Technology", "NASDAQ", "USD", "US", Sector.Technology),
        ("ON", "ON Semiconductor", "NASDAQ", "USD", "US", Sector.Technology),
        ("SWKS", "Skyworks Solutions", "NASDAQ", "USD", "US", Sector.Technology),
        ("AMAT", "Applied Materials Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("MU", "Micron Technology Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("ADI", "Analog Devices Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("NXPI", "NXP Semiconductors", "NASDAQ", "USD", "US", Sector.Technology),
        ("MRVL", "Marvell Technology", "NASDAQ", "USD", "US", Sector.Technology),
        ("FTNT", "Fortinet Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("ZS", "Zscaler Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("OKTA", "Okta Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("MDB", "MongoDB Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("TEAM", "Atlassian Corp.", "NASDAQ", "USD", "US", Sector.Technology),
        ("WDAY", "Workday Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("NOW", "ServiceNow Inc.", "NYSE", "USD", "US", Sector.Technology),
        ("HUBS", "HubSpot Inc.", "NYSE", "USD", "US", Sector.Technology),
        ("VEEV", "Veeva Systems Inc.", "NYSE", "USD", "US", Sector.Technology),
        ("SPLK", "Splunk Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("TTD", "The Trade Desk Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("BILL", "BILL Holdings Inc.", "NYSE", "USD", "US", Sector.Technology),
        ("GDDY", "GoDaddy Inc.", "NYSE", "USD", "US", Sector.Technology),
        ("TWLO", "Twilio Inc.", "NYSE", "USD", "US", Sector.Communication),
        ("U", "Unity Software Inc.", "NYSE", "USD", "US", Sector.Technology),
        ("PATH", "UiPath Inc.", "NYSE", "USD", "US", Sector.Technology),
        ("ESTC", "Elastic NV", "NYSE", "USD", "US", Sector.Technology),
        ("CFLT", "Confluent Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("S", "SentinelOne Inc.", "NYSE", "USD", "US", Sector.Technology),
        ("IOT", "Samsara Inc.", "NYSE", "USD", "US", Sector.Technology),
        ("WFC", "Wells Fargo & Co.", "NYSE", "USD", "US", Sector.Finance),
        ("BAC", "Bank of America Corp.", "NYSE", "USD", "US", Sector.Finance),
        ("C", "Citigroup Inc.", "NYSE", "USD", "US", Sector.Finance),
        ("MS", "Morgan Stanley", "NYSE", "USD", "US", Sector.Finance),
        ("SCHW", "Charles Schwab Corp.", "NYSE", "USD", "US", Sector.Finance),
        ("BLK", "BlackRock Inc.", "NYSE", "USD", "US", Sector.Finance),
        ("ICE", "Intercontinental Exchange", "NYSE", "USD", "US", Sector.Finance),
        ("CME", "CME Group Inc.", "NASDAQ", "USD", "US", Sector.Finance),
        ("CB", "Chubb Limited", "NYSE", "USD", "US", Sector.Finance),
        ("AON", "Aon plc", "NYSE", "USD", "US", Sector.Finance),
        ("MMC", "Marsh & McLennan", "NYSE", "USD", "US", Sector.Finance),
        ("PNC", "PNC Financial Services", "NYSE", "USD", "US", Sector.Finance),
        ("TFC", "Truist Financial Corp.", "NYSE", "USD", "US", Sector.Finance),
        ("USB", "U.S. Bancorp", "NYSE", "USD", "US", Sector.Finance),
        ("AXP", "American Express Co.", "NYSE", "USD", "US", Sector.Finance),
        ("SPGI", "S&P Global Inc.", "NYSE", "USD", "US", Sector.Finance),
        ("MCO", "Moody's Corporation", "NYSE", "USD", "US", Sector.Finance),
        ("FIS", "Fidelity National Info", "NYSE", "USD", "US", Sector.Finance),
        ("FISV", "Fiserv Inc.", "NYSE", "USD", "US", Sector.Finance),
        ("GPN", "Global Payments Inc.", "NYSE", "USD", "US", Sector.Finance),
        ("ABT", "Abbott Laboratories", "NYSE", "USD", "US", Sector.Healthcare),
        ("DHR", "Danaher Corporation", "NYSE", "USD", "US", Sector.Healthcare),
        ("ISRG", "Intuitive Surgical", "NASDAQ", "USD", "US", Sector.Healthcare),
        ("SYK", "Stryker Corporation", "NYSE", "USD", "US", Sector.Healthcare),
        ("MDT", "Medtronic plc", "NYSE", "USD", "US", Sector.Healthcare),
        ("BSX", "Boston Scientific Corp.", "NYSE", "USD", "US", Sector.Healthcare),
        ("EW", "Edwards Lifesciences", "NYSE", "USD", "US", Sector.Healthcare),
        ("ZTS", "Zoetis Inc.", "NYSE", "USD", "US", Sector.Healthcare),
        ("VRTX", "Vertex Pharmaceuticals", "NASDAQ", "USD", "US", Sector.Healthcare),
        ("REGN", "Regeneron Pharmaceuticals", "NASDAQ", "USD", "US", Sector.Healthcare),
        ("GILD", "Gilead Sciences Inc.", "NASDAQ", "USD", "US", Sector.Healthcare),
        ("BIIB", "Biogen Inc.", "NASDAQ", "USD", "US", Sector.Healthcare),
        ("ILMN", "Illumina Inc.", "NASDAQ", "USD", "US", Sector.Healthcare),
        ("DXCM", "DexCom Inc.", "NASDAQ", "USD", "US", Sector.Healthcare),
        ("A", "Agilent Technologies", "NYSE", "USD", "US", Sector.Healthcare),
        ("IQV", "IQVIA Holdings Inc.", "NYSE", "USD", "US", Sector.Healthcare),
        ("CI", "Cigna Group", "NYSE", "USD", "US", Sector.Healthcare),
        ("HUM", "Humana Inc.", "NYSE", "USD", "US", Sector.Healthcare),
        ("CNC", "Centene Corporation", "NYSE", "USD", "US", Sector.Healthcare),
        ("HCA", "HCA Healthcare Inc.", "NYSE", "USD", "US", Sector.Healthcare),
        ("LOW", "Lowe's Companies Inc.", "NYSE", "USD", "US", Sector.ConsumerDiscretionary),
        ("TJX", "TJX Companies Inc.", "NYSE", "USD", "US", Sector.ConsumerDiscretionary),
        ("ROST", "Ross Stores Inc.", "NASDAQ", "USD", "US", Sector.ConsumerDiscretionary),
        ("ORLY", "O'Reilly Automotive", "NASDAQ", "USD", "US", Sector.ConsumerDiscretionary),
        ("AZO", "AutoZone Inc.", "NYSE", "USD", "US", Sector.ConsumerDiscretionary),
        ("BKNG", "Booking Holdings", "NASDAQ", "USD", "US", Sector.ConsumerDiscretionary),
        ("MAR", "Marriott International", "NASDAQ", "USD", "US", Sector.ConsumerDiscretionary),
        ("HLT", "Hilton Worldwide", "NYSE", "USD", "US", Sector.ConsumerDiscretionary),
        ("CMG", "Chipotle Mexican Grill", "NYSE", "USD", "US", Sector.ConsumerDiscretionary),
        ("SBUX", "Starbucks Corporation", "NASDAQ", "USD", "US", Sector.ConsumerDiscretionary),
        ("MCD", "McDonald's Corporation", "NYSE", "USD", "US", Sector.ConsumerDiscretionary),
        ("YUM", "Yum! Brands Inc.", "NYSE", "USD", "US", Sector.ConsumerDiscretionary),
        ("DPZ", "Domino's Pizza Inc.", "NYSE", "USD", "US", Sector.ConsumerDiscretionary),
        ("DHI", "D.R. Horton Inc.", "NYSE", "USD", "US", Sector.ConsumerDiscretionary),
        ("LEN", "Lennar Corporation", "NYSE", "USD", "US", Sector.ConsumerDiscretionary),
        ("GM", "General Motors Co.", "NYSE", "USD", "US", Sector.ConsumerDiscretionary),
        ("F", "Ford Motor Company", "NYSE", "USD", "US", Sector.ConsumerDiscretionary),
        ("APTV", "Aptiv PLC", "NYSE", "USD", "US", Sector.ConsumerDiscretionary),
        ("LULU", "Lululemon Athletica", "NASDAQ", "USD", "US", Sector.ConsumerDiscretionary),
        ("DECK", "Deckers Outdoor Corp.", "NYSE", "USD", "US", Sector.ConsumerDiscretionary),
        ("CL", "Colgate-Palmolive Co.", "NYSE", "USD", "US", Sector.ConsumerStaples),
        ("KMB", "Kimberly-Clark Corp.", "NYSE", "USD", "US", Sector.ConsumerStaples),
        ("GIS", "General Mills Inc.", "NYSE", "USD", "US", Sector.ConsumerStaples),
        ("K", "Kellanova", "NYSE", "USD", "US", Sector.ConsumerStaples),
        ("HSY", "Hershey Company", "NYSE", "USD", "US", Sector.ConsumerStaples),
        ("MDLZ", "Mondelez International", "NASDAQ", "USD", "US", Sector.ConsumerStaples),
        ("MO", "Altria Group Inc.", "NYSE", "USD", "US", Sector.ConsumerStaples),
        ("PM", "Philip Morris International", "NYSE", "USD", "US", Sector.ConsumerStaples),
        ("EL", "Estee Lauder Cos.", "NYSE", "USD", "US", Sector.ConsumerStaples),
        ("STZ", "Constellation Brands", "NYSE", "USD", "US", Sector.ConsumerStaples),
        ("DE", "Deere & Company", "NYSE", "USD", "US", Sector.Industrials),
        ("LMT", "Lockheed Martin Corp.", "NYSE", "USD", "US", Sector.Industrials),
        ("NOC", "Northrop Grumman", "NYSE", "USD", "US", Sector.Industrials),
        ("GD", "General Dynamics Corp.", "NYSE", "USD", "US", Sector.Industrials),
        ("LHX", "L3Harris Technologies", "NYSE", "USD", "US", Sector.Industrials),
        ("TDG", "TransDigm Group Inc.", "NYSE", "USD", "US", Sector.Industrials),
        ("WM", "Waste Management Inc.", "NYSE", "USD", "US", Sector.Industrials),
        ("RSG", "Republic Services Inc.", "NYSE", "USD", "US", Sector.Industrials),
        ("EMR", "Emerson Electric Co.", "NYSE", "USD", "US", Sector.Industrials),
        ("ETN", "Eaton Corporation", "NYSE", "USD", "US", Sector.Industrials),
        ("ITW", "Illinois Tool Works", "NYSE", "USD", "US", Sector.Industrials),
        ("ROK", "Rockwell Automation", "NYSE", "USD", "US", Sector.Industrials),
        ("FDX", "FedEx Corporation", "NYSE", "USD", "US", Sector.Industrials),
        ("CTAS", "Cintas Corporation", "NASDAQ", "USD", "US", Sector.Industrials),
        ("PAYX", "Paychex Inc.", "NASDAQ", "USD", "US", Sector.Industrials),
        ("VRSK", "Verisk Analytics", "NASDAQ", "USD", "US", Sector.Industrials),
        ("CARR", "Carrier Global Corp.", "NYSE", "USD", "US", Sector.Industrials),
        ("OTIS", "Otis Worldwide Corp.", "NYSE", "USD", "US", Sector.Industrials),
        ("CSX", "CSX Corporation", "NASDAQ", "USD", "US", Sector.Industrials),
        ("NSC", "Norfolk Southern", "NYSE", "USD", "US", Sector.Industrials),
        ("OXY", "Occidental Petroleum", "NYSE", "USD", "US", Sector.Energy),
        ("COP", "ConocoPhillips", "NYSE", "USD", "US", Sector.Energy),
        ("EOG", "EOG Resources Inc.", "NYSE", "USD", "US", Sector.Energy),
        ("PXD", "Pioneer Natural Resources", "NYSE", "USD", "US", Sector.Energy),
        ("MPC", "Marathon Petroleum", "NYSE", "USD", "US", Sector.Energy),
        ("VLO", "Valero Energy Corp.", "NYSE", "USD", "US", Sector.Energy),
        ("PSX", "Phillips 66", "NYSE", "USD", "US", Sector.Energy),
        ("HES", "Hess Corporation", "NYSE", "USD", "US", Sector.Energy),
        ("DVN", "Devon Energy Corp.", "NYSE", "USD", "US", Sector.Energy),
        ("FANG", "Diamondback Energy", "NASDAQ", "USD", "US", Sector.Energy),
        ("EXC", "Exelon Corporation", "NASDAQ", "USD", "US", Sector.Utilities),
        ("D", "Dominion Energy Inc.", "NYSE", "USD", "US", Sector.Utilities),
        ("SRE", "Sempra Energy", "NYSE", "USD", "US", Sector.Utilities),
        ("AES", "AES Corporation", "NYSE", "USD", "US", Sector.Utilities),
        ("XEL", "Xcel Energy Inc.", "NASDAQ", "USD", "US", Sector.Utilities),
        ("WEC", "WEC Energy Group", "NYSE", "USD", "US", Sector.Utilities),
        ("ES2", "Eversource Energy", "NYSE", "USD", "US", Sector.Utilities),
        ("AWK", "American Water Works", "NYSE", "USD", "US", Sector.Utilities),
        ("LIN", "Linde plc", "NASDAQ", "USD", "US", Sector.Materials),
        ("DD", "DuPont de Nemours", "NYSE", "USD", "US", Sector.Materials),
        ("NEM", "Newmont Corporation", "NYSE", "USD", "US", Sector.Materials),
        ("NUE", "Nucor Corporation", "NYSE", "USD", "US", Sector.Materials),
        ("VMC", "Vulcan Materials Co.", "NYSE", "USD", "US", Sector.Materials),
        ("MLM", "Martin Marietta Materials", "NYSE", "USD", "US", Sector.Materials),
        ("ALB", "Albemarle Corporation", "NYSE", "USD", "US", Sector.Materials),
        ("IFF", "International Flavors", "NYSE", "USD", "US", Sector.Materials),
        ("CCI", "Crown Castle Inc.", "NYSE", "USD", "US", Sector.RealEstate),
        ("EQIX", "Equinix Inc.", "NASDAQ", "USD", "US", Sector.RealEstate),
        ("SPG", "Simon Property Group", "NYSE", "USD", "US", Sector.RealEstate),
        ("PSA", "Public Storage", "NYSE", "USD", "US", Sector.RealEstate),
        ("O", "Realty Income Corp.", "NYSE", "USD", "US", Sector.RealEstate),
        ("DLR", "Digital Realty Trust", "NYSE", "USD", "US", Sector.RealEstate),
        ("WELL", "Welltower Inc.", "NYSE", "USD", "US", Sector.RealEstate),
        ("ARE", "Alexandria Real Estate", "NYSE", "USD", "US", Sector.RealEstate),
        ("CHTR", "Charter Communications", "NASDAQ", "USD", "US", Sector.Communication),
        ("CMCSA", "Comcast Corporation", "NASDAQ", "USD", "US", Sector.Communication),
        ("TMUS", "T-Mobile US Inc.", "NASDAQ", "USD", "US", Sector.Communication),
        ("T", "AT&T Inc.", "NYSE", "USD", "US", Sector.Communication),
        ("VZ", "Verizon Communications", "NYSE", "USD", "US", Sector.Communication),
        ("EA", "Electronic Arts Inc.", "NASDAQ", "USD", "US", Sector.Communication),
        ("TTWO", "Take-Two Interactive", "NASDAQ", "USD", "US", Sector.Communication),
        ("MTCH", "Match Group Inc.", "NASDAQ", "USD", "US", Sector.Communication),
        ("WBD", "Warner Bros Discovery", "NASDAQ", "USD", "US", Sector.Communication),
        ("PARA", "Paramount Global", "NASDAQ", "USD", "US", Sector.Communication),
        ("ENPH", "Enphase Energy Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("SEDG", "SolarEdge Technologies", "NASDAQ", "USD", "US", Sector.Technology),
        ("FSLR", "First Solar Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("MPWR", "Monolithic Power Systems", "NASDAQ", "USD", "US", Sector.Technology),
        ("CDNS", "Cadence Design Systems", "NASDAQ", "USD", "US", Sector.Technology),
        ("SNPS", "Synopsys Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("ANSS", "ANSYS Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("PTC", "PTC Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("CPRT", "Copart Inc.", "NASDAQ", "USD", "US", Sector.Industrials),
        ("FAST", "Fastenal Company", "NASDAQ", "USD", "US", Sector.Industrials),
        ("ODFL", "Old Dominion Freight Line", "NASDAQ", "USD", "US", Sector.Industrials),
        ("PCAR", "PACCAR Inc.", "NASDAQ", "USD", "US", Sector.Industrials),
        ("URI", "United Rentals Inc.", "NYSE", "USD", "US", Sector.Industrials),
        ("PWR", "Quanta Services Inc.", "NYSE", "USD", "US", Sector.Industrials),
        ("AME", "AMETEK Inc.", "NYSE", "USD", "US", Sector.Industrials),
        ("IR", "Ingersoll Rand Inc.", "NYSE", "USD", "US", Sector.Industrials),
        ("DOV", "Dover Corporation", "NYSE", "USD", "US", Sector.Industrials),
        ("GWW", "W.W. Grainger Inc.", "NYSE", "USD", "US", Sector.Industrials),
        ("IDXX", "IDEXX Laboratories", "NASDAQ", "USD", "US", Sector.Healthcare),
        ("ALGN", "Align Technology", "NASDAQ", "USD", "US", Sector.Healthcare),
        ("WST", "West Pharmaceutical", "NYSE", "USD", "US", Sector.Healthcare),
        ("MTD", "Mettler-Toledo Intl.", "NYSE", "USD", "US", Sector.Healthcare),
        ("RMD", "ResMed Inc.", "NYSE", "USD", "US", Sector.Healthcare),
        ("HOLX", "Hologic Inc.", "NASDAQ", "USD", "US", Sector.Healthcare),
        ("TER", "Teradyne Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("ZBRA", "Zebra Technologies", "NASDAQ", "USD", "US", Sector.Technology),
        ("TRMB", "Trimble Inc.", "NASDAQ", "USD", "US", Sector.Technology),
        ("EPAM", "EPAM Systems Inc.", "NYSE", "USD", "US", Sector.Technology),
        ("PAYC", "Paycom Software", "NYSE", "USD", "US", Sector.Technology),
        ("PCTY", "Paylocity Holding", "NASDAQ", "USD", "US", Sector.Technology),
        ("MSCI", "MSCI Inc.", "NYSE", "USD", "US", Sector.Finance),
        ("NDAQ", "Nasdaq Inc.", "NASDAQ", "USD", "US", Sector.Finance),
        ("CBOE", "Cboe Global Markets", "NASDAQ", "USD", "US", Sector.Finance),
        ("TROW", "T. Rowe Price Group", "NASDAQ", "USD", "US", Sector.Finance),
        ("AMP", "Ameriprise Financial", "NYSE", "USD", "US", Sector.Finance),
        ("RJF", "Raymond James Financial", "NYSE", "USD", "US", Sector.Finance),
        ("MKTX", "MarketAxess Holdings", "NASDAQ", "USD", "US", Sector.Finance),
        ("LPLA", "LPL Financial Holdings", "NASDAQ", "USD", "US", Sector.Finance),
        ("WAT", "Waters Corporation", "NYSE", "USD", "US", Sector.Healthcare),
        ("BIO", "Bio-Rad Laboratories", "NYSE", "USD", "US", Sector.Healthcare),
        ("PKI", "PerkinElmer Inc.", "NYSE", "USD", "US", Sector.Healthcare),
        ("TFX", "Teleflex Inc.", "NYSE", "USD", "US", Sector.Healthcare),
        ("PODD", "Insulet Corporation", "NASDAQ", "USD", "US", Sector.Healthcare),
        ("INSP", "Inspire Medical Systems", "NYSE", "USD", "US", Sector.Healthcare),
    ];

    private static readonly (string Symbol, string Name, string Exchange, string Currency, AssetClass AC)[] EtfData =
    [
        ("SPY", "SPDR S&P 500 ETF Trust", "NYSE", "USD", AssetClass.Equities),
        ("QQQ", "Invesco QQQ Trust", "NASDAQ", "USD", AssetClass.Equities),
        ("IWM", "iShares Russell 2000 ETF", "NYSE", "USD", AssetClass.Equities),
        ("VTI", "Vanguard Total Stock Market ETF", "NYSE", "USD", AssetClass.Equities),
        ("VOO", "Vanguard S&P 500 ETF", "NYSE", "USD", AssetClass.Equities),
        ("EFA", "iShares MSCI EAFE ETF", "NYSE", "USD", AssetClass.Equities),
        ("EEM", "iShares MSCI Emerging Markets ETF", "NYSE", "USD", AssetClass.Equities),
        ("VWO", "Vanguard FTSE Emerging Markets ETF", "NYSE", "USD", AssetClass.Equities),
        ("GLD", "SPDR Gold Shares", "NYSE", "USD", AssetClass.Commodities),
        ("SLV", "iShares Silver Trust", "NYSE", "USD", AssetClass.Commodities),
        ("TLT", "iShares 20+ Year Treasury Bond ETF", "NASDAQ", "USD", AssetClass.FixedIncome),
        ("HYG", "iShares iBoxx High Yield Corporate Bond ETF", "NYSE", "USD", AssetClass.FixedIncome),
        ("LQD", "iShares Investment Grade Corporate Bond ETF", "NYSE", "USD", AssetClass.FixedIncome),
        ("AGG", "iShares Core U.S. Aggregate Bond ETF", "NYSE", "USD", AssetClass.FixedIncome),
        ("BND", "Vanguard Total Bond Market ETF", "NASDAQ", "USD", AssetClass.FixedIncome),
        ("XLF", "Financial Select Sector SPDR Fund", "NYSE", "USD", AssetClass.Equities),
        ("XLE", "Energy Select Sector SPDR Fund", "NYSE", "USD", AssetClass.Equities),
        ("XLK", "Technology Select Sector SPDR Fund", "NYSE", "USD", AssetClass.Equities),
        ("XLV", "Health Care Select Sector SPDR Fund", "NYSE", "USD", AssetClass.Equities),
        ("ARKK", "ARK Innovation ETF", "NYSE", "USD", AssetClass.Equities),
        ("VNQ", "Vanguard Real Estate ETF", "NYSE", "USD", AssetClass.Equities),
        ("SCHD", "Schwab US Dividend Equity ETF", "NYSE", "USD", AssetClass.Equities),
        ("DIA", "SPDR Dow Jones Industrial Average ETF", "NYSE", "USD", AssetClass.Equities),
        ("USO", "United States Oil Fund", "NYSE", "USD", AssetClass.Commodities),
        ("XLP", "Consumer Staples Select Sector SPDR", "NYSE", "USD", AssetClass.Equities),
        ("XLI", "Industrial Select Sector SPDR Fund", "NYSE", "USD", AssetClass.Equities),
        ("IEMG", "iShares Core MSCI Emerging Markets ETF", "NYSE", "USD", AssetClass.Equities),
        ("VEA", "Vanguard FTSE Developed Markets ETF", "NYSE", "USD", AssetClass.Equities),
        ("IVV", "iShares Core S&P 500 ETF", "NYSE", "USD", AssetClass.Equities),
        ("VXUS", "Vanguard Total International Stock ETF", "NASDAQ", "USD", AssetClass.Equities),
    ];

    private static readonly (string Symbol, string Name, string Currency)[] BondData =
    [
        ("US10Y", "US Treasury 10-Year Note", "USD"),
        ("US30Y", "US Treasury 30-Year Bond", "USD"),
        ("US5Y", "US Treasury 5-Year Note", "USD"),
        ("US2Y", "US Treasury 2-Year Note", "USD"),
        ("BUND10Y", "German Bund 10-Year", "EUR"),
        ("GILT10Y", "UK Gilt 10-Year", "GBP"),
        ("JGB10Y", "Japan Government Bond 10-Year", "JPY"),
        ("AAPL-BD", "Apple Inc. Corporate Bond 2030", "USD"),
        ("MSFT-BD", "Microsoft Corp. Bond 2028", "USD"),
        ("JPM-BD", "JPMorgan Chase Bond 2032", "USD"),
        ("GS-BD", "Goldman Sachs Bond 2029", "USD"),
        ("T-BD", "AT&T Corporate Bond 2031", "USD"),
        ("WMT-BD", "Walmart Inc. Bond 2027", "USD"),
        ("PG-BD", "Procter & Gamble Bond 2033", "USD"),
        ("XOM-BD", "Exxon Mobil Bond 2030", "USD"),
        ("MUNI-NY", "New York Municipal Bond", "USD"),
        ("MUNI-CA", "California Municipal Bond", "USD"),
        ("MUNI-TX", "Texas Municipal Bond", "USD"),
        ("EU-BD", "European Investment Bank Bond", "EUR"),
        ("IBRD-BD", "World Bank Bond 2029", "USD"),
        ("ADB-BD", "Asian Development Bank Bond", "USD"),
        ("BRL-BD", "Brazil Government Bond", "BRL"),
        ("ZAR-BD", "South Africa Government Bond", "ZAR"),
        ("INR-BD", "India Government Bond", "INR"),
        ("CAD-BD", "Canada Government Bond 10-Year", "CAD"),
        ("AUD-BD", "Australia Government Bond", "AUD"),
        ("CHF-BD", "Swiss Confederation Bond", "CHF"),
        ("CNY-BD", "China Government Bond 10-Year", "CNY"),
        ("KRW-BD", "South Korea Treasury Bond", "KRW"),
        ("NZD-BD", "New Zealand Government Bond", "NZD"),
    ];

    private static readonly (string Symbol, string Name)[] OptionData =
    [
        ("AAPL240621C", "AAPL Call Jun 2024 $190"),
        ("AAPL240621P", "AAPL Put Jun 2024 $180"),
        ("MSFT240621C", "MSFT Call Jun 2024 $420"),
        ("MSFT240621P", "MSFT Put Jun 2024 $400"),
        ("TSLA240621C", "TSLA Call Jun 2024 $250"),
        ("TSLA240621P", "TSLA Put Jun 2024 $220"),
        ("AMZN240621C", "AMZN Call Jun 2024 $185"),
        ("AMZN240621P", "AMZN Put Jun 2024 $170"),
        ("GOOGL240621C", "GOOGL Call Jun 2024 $175"),
        ("GOOGL240621P", "GOOGL Put Jun 2024 $160"),
        ("NVDA240621C", "NVDA Call Jun 2024 $900"),
        ("NVDA240621P", "NVDA Put Jun 2024 $800"),
        ("META240621C", "META Call Jun 2024 $500"),
        ("META240621P", "META Put Jun 2024 $460"),
        ("SPY240621C", "SPY Call Jun 2024 $520"),
        ("SPY240621P", "SPY Put Jun 2024 $500"),
        ("QQQ240621C", "QQQ Call Jun 2024 $450"),
        ("QQQ240621P", "QQQ Put Jun 2024 $430"),
        ("JPM240621C", "JPM Call Jun 2024 $200"),
        ("BA240621C", "BA Call Jun 2024 $200"),
        ("XOM240621C", "XOM Call Jun 2024 $115"),
        ("GLD240621C", "GLD Call Jun 2024 $220"),
        ("IWM240621C", "IWM Call Jun 2024 $210"),
        ("DIS240621C", "DIS Call Jun 2024 $115"),
        ("NFLX240621C", "NFLX Call Jun 2024 $650"),
        ("AMD240621C", "AMD Call Jun 2024 $180"),
        ("COIN240621C", "COIN Call Jun 2024 $260"),
        ("UBER240621C", "UBER Call Jun 2024 $80"),
        ("SQ240621C", "SQ Call Jun 2024 $85"),
        ("PLTR240621C", "PLTR Call Jun 2024 $25"),
        // ── Additional options (~400 more): Sep 2024, Dec 2024, Mar 2025, Jun 2025 ──
        // AAPL options
        ("AAPL240920C", "AAPL Call Sep 2024 $195"),
        ("AAPL240920P", "AAPL Put Sep 2024 $185"),
        ("AAPL241220C", "AAPL Call Dec 2024 $200"),
        ("AAPL241220P", "AAPL Put Dec 2024 $175"),
        ("AAPL250321C", "AAPL Call Mar 2025 $210"),
        ("AAPL250321P", "AAPL Put Mar 2025 $170"),
        ("AAPL250620C", "AAPL Call Jun 2025 $220"),
        ("AAPL250620P", "AAPL Put Jun 2025 $165"),
        // MSFT options
        ("MSFT240920C", "MSFT Call Sep 2024 $430"),
        ("MSFT240920P", "MSFT Put Sep 2024 $390"),
        ("MSFT241220C", "MSFT Call Dec 2024 $450"),
        ("MSFT241220P", "MSFT Put Dec 2024 $380"),
        ("MSFT250321C", "MSFT Call Mar 2025 $470"),
        ("MSFT250321P", "MSFT Put Mar 2025 $370"),
        ("MSFT250620C", "MSFT Call Jun 2025 $490"),
        ("MSFT250620P", "MSFT Put Jun 2025 $360"),
        // NVDA options
        ("NVDA240920C", "NVDA Call Sep 2024 $950"),
        ("NVDA240920P", "NVDA Put Sep 2024 $750"),
        ("NVDA241220C", "NVDA Call Dec 2024 $1000"),
        ("NVDA241220P", "NVDA Put Dec 2024 $700"),
        ("NVDA250321C", "NVDA Call Mar 2025 $1100"),
        ("NVDA250321P", "NVDA Put Mar 2025 $650"),
        ("NVDA250620C", "NVDA Call Jun 2025 $1200"),
        ("NVDA250620P", "NVDA Put Jun 2025 $600"),
        // TSLA options
        ("TSLA240920C", "TSLA Call Sep 2024 $260"),
        ("TSLA240920P", "TSLA Put Sep 2024 $210"),
        ("TSLA241220C", "TSLA Call Dec 2024 $280"),
        ("TSLA241220P", "TSLA Put Dec 2024 $200"),
        ("TSLA250321C", "TSLA Call Mar 2025 $300"),
        ("TSLA250321P", "TSLA Put Mar 2025 $190"),
        ("TSLA250620C", "TSLA Call Jun 2025 $320"),
        ("TSLA250620P", "TSLA Put Jun 2025 $180"),
        // AMZN options
        ("AMZN240920C", "AMZN Call Sep 2024 $195"),
        ("AMZN240920P", "AMZN Put Sep 2024 $165"),
        ("AMZN241220C", "AMZN Call Dec 2024 $210"),
        ("AMZN241220P", "AMZN Put Dec 2024 $155"),
        ("AMZN250321C", "AMZN Call Mar 2025 $220"),
        ("AMZN250321P", "AMZN Put Mar 2025 $150"),
        ("AMZN250620C", "AMZN Call Jun 2025 $230"),
        ("AMZN250620P", "AMZN Put Jun 2025 $145"),
        // GOOGL options
        ("GOOGL240920C", "GOOGL Call Sep 2024 $180"),
        ("GOOGL240920P", "GOOGL Put Sep 2024 $155"),
        ("GOOGL241220C", "GOOGL Call Dec 2024 $190"),
        ("GOOGL241220P", "GOOGL Put Dec 2024 $150"),
        ("GOOGL250321C", "GOOGL Call Mar 2025 $200"),
        ("GOOGL250321P", "GOOGL Put Mar 2025 $145"),
        ("GOOGL250620C", "GOOGL Call Jun 2025 $210"),
        ("GOOGL250620P", "GOOGL Put Jun 2025 $140"),
        // META options
        ("META240920C", "META Call Sep 2024 $520"),
        ("META240920P", "META Put Sep 2024 $450"),
        ("META241220C", "META Call Dec 2024 $550"),
        ("META241220P", "META Put Dec 2024 $430"),
        ("META250321C", "META Call Mar 2025 $580"),
        ("META250321P", "META Put Mar 2025 $420"),
        ("META250620C", "META Call Jun 2025 $600"),
        ("META250620P", "META Put Jun 2025 $400"),
        // JPM options
        ("JPM240920C", "JPM Call Sep 2024 $210"),
        ("JPM240920P", "JPM Put Sep 2024 $185"),
        ("JPM241220C", "JPM Call Dec 2024 $220"),
        ("JPM241220P", "JPM Put Dec 2024 $180"),
        ("JPM250321C", "JPM Call Mar 2025 $230"),
        ("JPM250321P", "JPM Put Mar 2025 $175"),
        ("JPM250620C", "JPM Call Jun 2025 $240"),
        ("JPM250620P", "JPM Put Jun 2025 $170"),
        // V options
        ("V240920C", "V Call Sep 2024 $290"),
        ("V240920P", "V Put Sep 2024 $265"),
        ("V241220C", "V Call Dec 2024 $300"),
        ("V241220P", "V Put Dec 2024 $260"),
        ("V250321C", "V Call Mar 2025 $310"),
        ("V250321P", "V Put Mar 2025 $255"),
        // UNH options
        ("UNH240920C", "UNH Call Sep 2024 $540"),
        ("UNH240920P", "UNH Put Sep 2024 $500"),
        ("UNH241220C", "UNH Call Dec 2024 $560"),
        ("UNH241220P", "UNH Put Dec 2024 $490"),
        ("UNH250321C", "UNH Call Mar 2025 $580"),
        ("UNH250321P", "UNH Put Mar 2025 $480"),
        // MA options
        ("MA240920C", "MA Call Sep 2024 $470"),
        ("MA240920P", "MA Put Sep 2024 $440"),
        ("MA241220C", "MA Call Dec 2024 $490"),
        ("MA241220P", "MA Put Dec 2024 $430"),
        ("MA250321C", "MA Call Mar 2025 $510"),
        ("MA250321P", "MA Put Mar 2025 $420"),
        // HD options
        ("HD240920C", "HD Call Sep 2024 $360"),
        ("HD240920P", "HD Put Sep 2024 $330"),
        ("HD241220C", "HD Call Dec 2024 $380"),
        ("HD241220P", "HD Put Dec 2024 $320"),
        ("HD250321C", "HD Call Mar 2025 $400"),
        ("HD250321P", "HD Put Mar 2025 $310"),
        // SPY options (heavy volume)
        ("SPY240920C", "SPY Call Sep 2024 $530"),
        ("SPY240920P", "SPY Put Sep 2024 $495"),
        ("SPY241220C", "SPY Call Dec 2024 $550"),
        ("SPY241220P", "SPY Put Dec 2024 $480"),
        ("SPY250321C", "SPY Call Mar 2025 $570"),
        ("SPY250321P", "SPY Put Mar 2025 $470"),
        ("SPY250620C", "SPY Call Jun 2025 $590"),
        ("SPY250620P", "SPY Put Jun 2025 $460"),
        ("SPY240621C480", "SPY Call Jun 2024 $480"),
        ("SPY240621P460", "SPY Put Jun 2024 $460"),
        ("SPY240621C540", "SPY Call Jun 2024 $540"),
        ("SPY240621P540", "SPY Put Jun 2024 $540"),
        // QQQ options
        ("QQQ240920C", "QQQ Call Sep 2024 $460"),
        ("QQQ240920P", "QQQ Put Sep 2024 $420"),
        ("QQQ241220C", "QQQ Call Dec 2024 $480"),
        ("QQQ241220P", "QQQ Put Dec 2024 $410"),
        ("QQQ250321C", "QQQ Call Mar 2025 $500"),
        ("QQQ250321P", "QQQ Put Mar 2025 $400"),
        ("QQQ250620C", "QQQ Call Jun 2025 $520"),
        ("QQQ250620P", "QQQ Put Jun 2025 $390"),
        // IWM options
        ("IWM240920C", "IWM Call Sep 2024 $220"),
        ("IWM240920P", "IWM Put Sep 2024 $195"),
        ("IWM241220C", "IWM Call Dec 2024 $230"),
        ("IWM241220P", "IWM Put Dec 2024 $190"),
        ("IWM250321C", "IWM Call Mar 2025 $240"),
        ("IWM250321P", "IWM Put Mar 2025 $185"),
        // NFLX options
        ("NFLX240920C", "NFLX Call Sep 2024 $680"),
        ("NFLX240920P", "NFLX Put Sep 2024 $620"),
        ("NFLX241220C", "NFLX Call Dec 2024 $720"),
        ("NFLX241220P", "NFLX Put Dec 2024 $600"),
        ("NFLX250321C", "NFLX Call Mar 2025 $750"),
        ("NFLX250321P", "NFLX Put Mar 2025 $580"),
        // AMD options
        ("AMD240920C", "AMD Call Sep 2024 $190"),
        ("AMD240920P", "AMD Put Sep 2024 $160"),
        ("AMD241220C", "AMD Call Dec 2024 $200"),
        ("AMD241220P", "AMD Put Dec 2024 $150"),
        ("AMD250321C", "AMD Call Mar 2025 $210"),
        ("AMD250321P", "AMD Put Mar 2025 $145"),
        ("AMD250620C", "AMD Call Jun 2025 $220"),
        ("AMD250620P", "AMD Put Jun 2025 $140"),
        // CRM options
        ("CRM240920C", "CRM Call Sep 2024 $280"),
        ("CRM240920P", "CRM Put Sep 2024 $250"),
        ("CRM241220C", "CRM Call Dec 2024 $300"),
        ("CRM241220P", "CRM Put Dec 2024 $240"),
        ("CRM250321C", "CRM Call Mar 2025 $320"),
        ("CRM250321P", "CRM Put Mar 2025 $230"),
        // AVGO options
        ("AVGO240920C", "AVGO Call Sep 2024 $1400"),
        ("AVGO240920P", "AVGO Put Sep 2024 $1200"),
        ("AVGO241220C", "AVGO Call Dec 2024 $1500"),
        ("AVGO241220P", "AVGO Put Dec 2024 $1100"),
        ("AVGO250321C", "AVGO Call Mar 2025 $1600"),
        ("AVGO250321P", "AVGO Put Mar 2025 $1000"),
        // LLY options
        ("LLY240920C", "LLY Call Sep 2024 $800"),
        ("LLY240920P", "LLY Put Sep 2024 $720"),
        ("LLY241220C", "LLY Call Dec 2024 $850"),
        ("LLY241220P", "LLY Put Dec 2024 $700"),
        ("LLY250321C", "LLY Call Mar 2025 $900"),
        ("LLY250321P", "LLY Put Mar 2025 $680"),
        // COST options
        ("COST240920C", "COST Call Sep 2024 $750"),
        ("COST240920P", "COST Put Sep 2024 $700"),
        ("COST241220C", "COST Call Dec 2024 $780"),
        ("COST241220P", "COST Put Dec 2024 $680"),
        // WMT options
        ("WMT240920C", "WMT Call Sep 2024 $180"),
        ("WMT240920P", "WMT Put Sep 2024 $160"),
        ("WMT241220C", "WMT Call Dec 2024 $190"),
        ("WMT241220P", "WMT Put Dec 2024 $155"),
        // PG options
        ("PG240920C", "PG Call Sep 2024 $175"),
        ("PG240920P", "PG Put Sep 2024 $155"),
        ("PG241220C", "PG Call Dec 2024 $185"),
        ("PG241220P", "PG Put Dec 2024 $150"),
        // MRK options
        ("MRK240920C", "MRK Call Sep 2024 $135"),
        ("MRK240920P", "MRK Put Sep 2024 $115"),
        ("MRK241220C", "MRK Call Dec 2024 $140"),
        ("MRK241220P", "MRK Put Dec 2024 $110"),
        // ABBV options
        ("ABBV240920C", "ABBV Call Sep 2024 $180"),
        ("ABBV240920P", "ABBV Put Sep 2024 $160"),
        ("ABBV241220C", "ABBV Call Dec 2024 $190"),
        ("ABBV241220P", "ABBV Put Dec 2024 $155"),
        // PEP options
        ("PEP240920C", "PEP Call Sep 2024 $180"),
        ("PEP240920P", "PEP Put Sep 2024 $160"),
        ("PEP241220C", "PEP Call Dec 2024 $190"),
        ("PEP241220P", "PEP Put Dec 2024 $155"),
        // KO options
        ("KO240920C", "KO Call Sep 2024 $65"),
        ("KO240920P", "KO Put Sep 2024 $57"),
        ("KO241220C", "KO Call Dec 2024 $68"),
        ("KO241220P", "KO Put Dec 2024 $55"),
        // XOM options
        ("XOM240920C", "XOM Call Sep 2024 $120"),
        ("XOM240920P", "XOM Put Sep 2024 $100"),
        ("XOM241220C", "XOM Call Dec 2024 $125"),
        ("XOM241220P", "XOM Put Dec 2024 $95"),
        // BA options
        ("BA240920C", "BA Call Sep 2024 $210"),
        ("BA240920P", "BA Put Sep 2024 $180"),
        ("BA241220C", "BA Call Dec 2024 $220"),
        ("BA241220P", "BA Put Dec 2024 $175"),
        // GS options
        ("GS240920C", "GS Call Sep 2024 $440"),
        ("GS240920P", "GS Put Sep 2024 $400"),
        ("GS241220C", "GS Call Dec 2024 $460"),
        ("GS241220P", "GS Put Dec 2024 $390"),
        // DIS options
        ("DIS240920C", "DIS Call Sep 2024 $120"),
        ("DIS240920P", "DIS Put Sep 2024 $100"),
        ("DIS241220C", "DIS Call Dec 2024 $130"),
        ("DIS241220P", "DIS Put Dec 2024 $95"),
        // COIN options
        ("COIN240920C", "COIN Call Sep 2024 $280"),
        ("COIN240920P", "COIN Put Sep 2024 $220"),
        ("COIN241220C", "COIN Call Dec 2024 $300"),
        ("COIN241220P", "COIN Put Dec 2024 $200"),
        // UBER options
        ("UBER240920C", "UBER Call Sep 2024 $85"),
        ("UBER240920P", "UBER Put Sep 2024 $65"),
        ("UBER241220C", "UBER Call Dec 2024 $90"),
        ("UBER241220P", "UBER Put Dec 2024 $60"),
        // PYPL options
        ("PYPL240920C", "PYPL Call Sep 2024 $75"),
        ("PYPL240920P", "PYPL Put Sep 2024 $58"),
        ("PYPL241220C", "PYPL Call Dec 2024 $80"),
        ("PYPL241220P", "PYPL Put Dec 2024 $55"),
        // SQ options
        ("SQ240920C", "SQ Call Sep 2024 $90"),
        ("SQ240920P", "SQ Put Sep 2024 $70"),
        ("SQ241220C", "SQ Call Dec 2024 $95"),
        ("SQ241220P", "SQ Put Dec 2024 $65"),
        // ABNB options
        ("ABNB240920C", "ABNB Call Sep 2024 $160"),
        ("ABNB240920P", "ABNB Put Sep 2024 $135"),
        ("ABNB241220C", "ABNB Call Dec 2024 $170"),
        ("ABNB241220P", "ABNB Put Dec 2024 $130"),
        // SNOW options
        ("SNOW240920C", "SNOW Call Sep 2024 $180"),
        ("SNOW240920P", "SNOW Put Sep 2024 $150"),
        ("SNOW241220C", "SNOW Call Dec 2024 $200"),
        ("SNOW241220P", "SNOW Put Dec 2024 $140"),
        // PLTR options
        ("PLTR240920C", "PLTR Call Sep 2024 $28"),
        ("PLTR240920P", "PLTR Put Sep 2024 $20"),
        ("PLTR241220C", "PLTR Call Dec 2024 $32"),
        ("PLTR241220P", "PLTR Put Dec 2024 $18"),
        ("PLTR250321C", "PLTR Call Mar 2025 $35"),
        ("PLTR250321P", "PLTR Put Mar 2025 $16"),
        // MRNA options
        ("MRNA240920C", "MRNA Call Sep 2024 $130"),
        ("MRNA240920P", "MRNA Put Sep 2024 $95"),
        ("MRNA241220C", "MRNA Call Dec 2024 $140"),
        ("MRNA241220P", "MRNA Put Dec 2024 $85"),
        // PANW options
        ("PANW240920C", "PANW Call Sep 2024 $320"),
        ("PANW240920P", "PANW Put Sep 2024 $280"),
        ("PANW241220C", "PANW Call Dec 2024 $340"),
        ("PANW241220P", "PANW Put Dec 2024 $270"),
        // CRWD options
        ("CRWD240920C", "CRWD Call Sep 2024 $350"),
        ("CRWD240920P", "CRWD Put Sep 2024 $300"),
        ("CRWD241220C", "CRWD Call Dec 2024 $370"),
        ("CRWD241220P", "CRWD Put Dec 2024 $290"),
        // GLD options
        ("GLD240920C", "GLD Call Sep 2024 $230"),
        ("GLD240920P", "GLD Put Sep 2024 $210"),
        ("GLD241220C", "GLD Call Dec 2024 $240"),
        ("GLD241220P", "GLD Put Dec 2024 $200"),
        // BAC options
        ("BAC240920C", "BAC Call Sep 2024 $40"),
        ("BAC240920P", "BAC Put Sep 2024 $33"),
        ("BAC241220C", "BAC Call Dec 2024 $42"),
        ("BAC241220P", "BAC Put Dec 2024 $31"),
        // WFC options
        ("WFC240920C", "WFC Call Sep 2024 $55"),
        ("WFC240920P", "WFC Put Sep 2024 $45"),
        ("WFC241220C", "WFC Call Dec 2024 $58"),
        ("WFC241220P", "WFC Put Dec 2024 $43"),
        // C options
        ("C240920C", "C Call Sep 2024 $60"),
        ("C240920P", "C Put Sep 2024 $50"),
        ("C241220C", "C Call Dec 2024 $65"),
        ("C241220P", "C Put Dec 2024 $48"),
        // MS options
        ("MS240920C", "MS Call Sep 2024 $100"),
        ("MS240920P", "MS Put Sep 2024 $85"),
        ("MS241220C", "MS Call Dec 2024 $105"),
        ("MS241220P", "MS Put Dec 2024 $82"),
        // INTC options
        ("INTC240920C", "INTC Call Sep 2024 $35"),
        ("INTC240920P", "INTC Put Sep 2024 $27"),
        ("INTC241220C", "INTC Call Dec 2024 $38"),
        ("INTC241220P", "INTC Put Dec 2024 $25"),
        // QCOM options
        ("QCOM240920C", "QCOM Call Sep 2024 $200"),
        ("QCOM240920P", "QCOM Put Sep 2024 $170"),
        ("QCOM241220C", "QCOM Call Dec 2024 $210"),
        ("QCOM241220P", "QCOM Put Dec 2024 $165"),
        // ADBE options
        ("ADBE240920C", "ADBE Call Sep 2024 $560"),
        ("ADBE240920P", "ADBE Put Sep 2024 $500"),
        ("ADBE241220C", "ADBE Call Dec 2024 $580"),
        ("ADBE241220P", "ADBE Put Dec 2024 $490"),
        // ORCL options
        ("ORCL240920C", "ORCL Call Sep 2024 $130"),
        ("ORCL240920P", "ORCL Put Sep 2024 $115"),
        ("ORCL241220C", "ORCL Call Dec 2024 $140"),
        ("ORCL241220P", "ORCL Put Dec 2024 $110"),
        // NOW options
        ("NOW240920C", "NOW Call Sep 2024 $800"),
        ("NOW240920P", "NOW Put Sep 2024 $720"),
        ("NOW241220C", "NOW Call Dec 2024 $850"),
        ("NOW241220P", "NOW Put Dec 2024 $700"),
        // LOW options
        ("LOW240920C", "LOW Call Sep 2024 $250"),
        ("LOW240920P", "LOW Put Sep 2024 $225"),
        ("LOW241220C", "LOW Call Dec 2024 $260"),
        ("LOW241220P", "LOW Put Dec 2024 $220"),
        // SBUX options
        ("SBUX240920C", "SBUX Call Sep 2024 $85"),
        ("SBUX240920P", "SBUX Put Sep 2024 $72"),
        ("SBUX241220C", "SBUX Call Dec 2024 $90"),
        ("SBUX241220P", "SBUX Put Dec 2024 $68"),
        // MCD options
        ("MCD240920C", "MCD Call Sep 2024 $280"),
        ("MCD240920P", "MCD Put Sep 2024 $255"),
        ("MCD241220C", "MCD Call Dec 2024 $290"),
        ("MCD241220P", "MCD Put Dec 2024 $250"),
        // ISRG options
        ("ISRG240920C", "ISRG Call Sep 2024 $420"),
        ("ISRG240920P", "ISRG Put Sep 2024 $380"),
        ("ISRG241220C", "ISRG Call Dec 2024 $440"),
        ("ISRG241220P", "ISRG Put Dec 2024 $370"),
        // BKNG options
        ("BKNG240920C", "BKNG Call Sep 2024 $3800"),
        ("BKNG240920P", "BKNG Put Sep 2024 $3500"),
        ("BKNG241220C", "BKNG Call Dec 2024 $4000"),
        ("BKNG241220P", "BKNG Put Dec 2024 $3400"),
        // COP options
        ("COP240920C", "COP Call Sep 2024 $125"),
        ("COP240920P", "COP Put Sep 2024 $108"),
        ("COP241220C", "COP Call Dec 2024 $130"),
        ("COP241220P", "COP Put Dec 2024 $105"),
        // CMG options
        ("CMG240920C", "CMG Call Sep 2024 $3100"),
        ("CMG240920P", "CMG Put Sep 2024 $2800"),
        ("CMG241220C", "CMG Call Dec 2024 $3300"),
        ("CMG241220P", "CMG Put Dec 2024 $2700"),
        // LULU options
        ("LULU240920C", "LULU Call Sep 2024 $400"),
        ("LULU240920P", "LULU Put Sep 2024 $350"),
        ("LULU241220C", "LULU Call Dec 2024 $420"),
        ("LULU241220P", "LULU Put Dec 2024 $340"),
        // GM options
        ("GM240920C", "GM Call Sep 2024 $48"),
        ("GM240920P", "GM Put Sep 2024 $38"),
        ("GM241220C", "GM Call Dec 2024 $50"),
        ("GM241220P", "GM Put Dec 2024 $36"),
        // F options
        ("F240920C", "F Call Sep 2024 $14"),
        ("F240920P", "F Put Sep 2024 $11"),
        ("F241220C", "F Call Dec 2024 $15"),
        ("F241220P", "F Put Dec 2024 $10"),
        // LMT options
        ("LMT240920C", "LMT Call Sep 2024 $460"),
        ("LMT240920P", "LMT Put Sep 2024 $430"),
        ("LMT241220C", "LMT Call Dec 2024 $480"),
        ("LMT241220P", "LMT Put Dec 2024 $420"),
        // DE options
        ("DE240920C", "DE Call Sep 2024 $400"),
        ("DE240920P", "DE Put Sep 2024 $370"),
        ("DE241220C", "DE Call Dec 2024 $420"),
        ("DE241220P", "DE Put Dec 2024 $360"),
        // NEE options
        ("NEE240920C", "NEE Call Sep 2024 $72"),
        ("NEE240920P", "NEE Put Sep 2024 $62"),
        ("NEE241220C", "NEE Call Dec 2024 $75"),
        ("NEE241220P", "NEE Put Dec 2024 $60"),
        // DDOG options
        ("DDOG240920C", "DDOG Call Sep 2024 $135"),
        ("DDOG240920P", "DDOG Put Sep 2024 $110"),
        ("DDOG241220C", "DDOG Call Dec 2024 $145"),
        ("DDOG241220P", "DDOG Put Dec 2024 $105"),
        // NET options
        ("NET240920C", "NET Call Sep 2024 $95"),
        ("NET240920P", "NET Put Sep 2024 $75"),
        ("NET241220C", "NET Call Dec 2024 $100"),
        ("NET241220P", "NET Put Dec 2024 $70"),
        // RIVN options
        ("RIVN240920C", "RIVN Call Sep 2024 $18"),
        ("RIVN240920P", "RIVN Put Sep 2024 $12"),
        ("RIVN241220C", "RIVN Call Dec 2024 $20"),
        ("RIVN241220P", "RIVN Put Dec 2024 $10"),
        // ZM options
        ("ZM240920C", "ZM Call Sep 2024 $70"),
        ("ZM240920P", "ZM Put Sep 2024 $55"),
        ("ZM241220C", "ZM Call Dec 2024 $75"),
        ("ZM241220P", "ZM Put Dec 2024 $50"),
        // SPOT options
        ("SPOT240920C", "SPOT Call Sep 2024 $310"),
        ("SPOT240920P", "SPOT Put Sep 2024 $270"),
        ("SPOT241220C", "SPOT Call Dec 2024 $330"),
        ("SPOT241220P", "SPOT Put Dec 2024 $260"),
        // RBLX options
        ("RBLX240920C", "RBLX Call Sep 2024 $42"),
        ("RBLX240920P", "RBLX Put Sep 2024 $32"),
        ("RBLX241220C", "RBLX Call Dec 2024 $45"),
        ("RBLX241220P", "RBLX Put Dec 2024 $30"),
        // SNAP options
        ("SNAP240920C", "SNAP Call Sep 2024 $14"),
        ("SNAP240920P", "SNAP Put Sep 2024 $9"),
        ("SNAP241220C", "SNAP Call Dec 2024 $16"),
        ("SNAP241220P", "SNAP Put Dec 2024 $8"),
        // ROKU options
        ("ROKU240920C", "ROKU Call Sep 2024 $70"),
        ("ROKU240920P", "ROKU Put Sep 2024 $52"),
        ("ROKU241220C", "ROKU Call Dec 2024 $75"),
        ("ROKU241220P", "ROKU Put Dec 2024 $48"),
        // AXP options
        ("AXP240920C", "AXP Call Sep 2024 $240"),
        ("AXP240920P", "AXP Put Sep 2024 $215"),
        ("AXP241220C", "AXP Call Dec 2024 $250"),
        ("AXP241220P", "AXP Put Dec 2024 $210"),
        // CVX options
        ("CVX240920C", "CVX Call Sep 2024 $165"),
        ("CVX240920P", "CVX Put Sep 2024 $148"),
        ("CVX241220C", "CVX Call Dec 2024 $170"),
        ("CVX241220P", "CVX Put Dec 2024 $145"),
        // TMO options
        ("TMO240920C", "TMO Call Sep 2024 $580"),
        ("TMO240920P", "TMO Put Sep 2024 $540"),
        ("TMO241220C", "TMO Call Dec 2024 $600"),
        ("TMO241220P", "TMO Put Dec 2024 $530"),
        // CSCO options
        ("CSCO240920C", "CSCO Call Sep 2024 $52"),
        ("CSCO240920P", "CSCO Put Sep 2024 $45"),
        ("CSCO241220C", "CSCO Call Dec 2024 $55"),
        ("CSCO241220P", "CSCO Put Dec 2024 $43"),
        // ACN options
        ("ACN240920C", "ACN Call Sep 2024 $340"),
        ("ACN240920P", "ACN Put Sep 2024 $310"),
        ("ACN241220C", "ACN Call Dec 2024 $355"),
        ("ACN241220P", "ACN Put Dec 2024 $300"),
        // VRTX options
        ("VRTX240920C", "VRTX Call Sep 2024 $450"),
        ("VRTX240920P", "VRTX Put Sep 2024 $410"),
        ("VRTX241220C", "VRTX Call Dec 2024 $470"),
        ("VRTX241220P", "VRTX Put Dec 2024 $400"),
        // REGN options
        ("REGN240920C", "REGN Call Sep 2024 $1000"),
        ("REGN240920P", "REGN Put Sep 2024 $920"),
        ("REGN241220C", "REGN Call Dec 2024 $1050"),
        ("REGN241220P", "REGN Put Dec 2024 $900"),
    ];

    private static readonly (string Symbol, string Name, string Currency, AssetClass AC)[] FutureData =
    [
        ("ES", "E-mini S&P 500 Future", "USD", AssetClass.Derivatives),
        ("NQ", "E-mini NASDAQ-100 Future", "USD", AssetClass.Derivatives),
        ("YM", "E-mini Dow Jones Future", "USD", AssetClass.Derivatives),
        ("RTY", "E-mini Russell 2000 Future", "USD", AssetClass.Derivatives),
        ("CL1", "Crude Oil WTI Future", "USD", AssetClass.Commodities),
        ("NG", "Natural Gas Future", "USD", AssetClass.Commodities),
        ("GC", "Gold Future", "USD", AssetClass.Commodities),
        ("SI", "Silver Future", "USD", AssetClass.Commodities),
        ("HG", "Copper Future", "USD", AssetClass.Commodities),
        ("PL", "Platinum Future", "USD", AssetClass.Commodities),
        ("ZB", "US Treasury Bond Future", "USD", AssetClass.FixedIncome),
        ("ZN", "US 10-Year T-Note Future", "USD", AssetClass.FixedIncome),
        ("ZC", "Corn Future", "USD", AssetClass.Commodities),
        ("ZW", "Wheat Future", "USD", AssetClass.Commodities),
        ("ZS1", "Soybean Future", "USD", AssetClass.Commodities),
        ("KC", "Coffee Future", "USD", AssetClass.Commodities),
        ("CT", "Cotton Future", "USD", AssetClass.Commodities),
        ("SB", "Sugar Future", "USD", AssetClass.Commodities),
        ("LE", "Live Cattle Future", "USD", AssetClass.Commodities),
        ("HE", "Lean Hogs Future", "USD", AssetClass.Commodities),
        ("6E", "Euro FX Future", "USD", AssetClass.ForeignExchange),
        ("6B", "British Pound Future", "USD", AssetClass.ForeignExchange),
        ("6J", "Japanese Yen Future", "USD", AssetClass.ForeignExchange),
        ("6A", "Australian Dollar Future", "USD", AssetClass.ForeignExchange),
        ("6C", "Canadian Dollar Future", "USD", AssetClass.ForeignExchange),
        ("VX", "VIX Future", "USD", AssetClass.Derivatives),
        ("BTC", "Bitcoin Future", "USD", AssetClass.Derivatives),
        ("ETH", "Ether Future", "USD", AssetClass.Derivatives),
        ("FGBL", "Euro-Bund Future", "EUR", AssetClass.FixedIncome),
        ("NK", "Nikkei 225 Future", "JPY", AssetClass.Derivatives),
    ];

    private static readonly (string Symbol, string Name, string BaseCurrency, string QuoteCurrency)[] ForexData =
    [
        ("EURUSD", "Euro / US Dollar", "EUR", "USD"),
        ("GBPUSD", "British Pound / US Dollar", "GBP", "USD"),
        ("USDJPY", "US Dollar / Japanese Yen", "USD", "JPY"),
        ("USDCHF", "US Dollar / Swiss Franc", "USD", "CHF"),
        ("AUDUSD", "Australian Dollar / US Dollar", "AUD", "USD"),
        ("USDCAD", "US Dollar / Canadian Dollar", "USD", "CAD"),
        ("NZDUSD", "New Zealand Dollar / US Dollar", "NZD", "USD"),
        ("EURGBP", "Euro / British Pound", "EUR", "GBP"),
        ("EURJPY", "Euro / Japanese Yen", "EUR", "JPY"),
        ("GBPJPY", "British Pound / Japanese Yen", "GBP", "JPY"),
        ("EURCHF", "Euro / Swiss Franc", "EUR", "CHF"),
        ("AUDJPY", "Australian Dollar / Japanese Yen", "AUD", "JPY"),
        ("EURAUD", "Euro / Australian Dollar", "EUR", "AUD"),
        ("EURCAD", "Euro / Canadian Dollar", "EUR", "CAD"),
        ("GBPCHF", "British Pound / Swiss Franc", "GBP", "CHF"),
        ("CADJPY", "Canadian Dollar / Japanese Yen", "CAD", "JPY"),
        ("AUDCAD", "Australian Dollar / Canadian Dollar", "AUD", "CAD"),
        ("AUDCHF", "Australian Dollar / Swiss Franc", "AUD", "CHF"),
        ("NZDJPY", "New Zealand Dollar / Japanese Yen", "NZD", "JPY"),
        ("USDHKD", "US Dollar / Hong Kong Dollar", "USD", "HKD"),
        ("USDSGD", "US Dollar / Singapore Dollar", "USD", "SGD"),
        ("USDINR", "US Dollar / Indian Rupee", "USD", "INR"),
        ("USDCNY", "US Dollar / Chinese Yuan", "USD", "CNY"),
        ("USDBRL", "US Dollar / Brazilian Real", "USD", "BRL"),
        ("USDZAR", "US Dollar / South African Rand", "USD", "ZAR"),
    ];

    private static readonly (string Symbol, string Name)[] CfdData =
    [
        ("US500", "US 500 Index CFD"), ("US100", "US Tech 100 CFD"),
        ("US30", "US Wall Street 30 CFD"), ("UK100", "UK 100 Index CFD"),
        ("DE40", "Germany 40 CFD"), ("JP225", "Japan 225 CFD"),
        ("HK50", "Hong Kong 50 CFD"), ("AU200", "Australia 200 CFD"),
        ("EU50", "Euro Stoxx 50 CFD"), ("FR40", "France 40 CFD"),
        ("XAUUSD", "Gold CFD (USD/oz)"), ("XAGUSD", "Silver CFD (USD/oz)"),
        ("USOIL", "US Crude Oil CFD"), ("UKOIL", "Brent Crude Oil CFD"),
        ("NATGAS", "Natural Gas CFD"),
    ];

    private static readonly (string Symbol, string Name, string Currency)[] MutualFundData =
    [
        ("VFIAX", "Vanguard 500 Index Fund Admiral", "USD"),
        ("FXAIX", "Fidelity 500 Index Fund", "USD"),
        ("VTSAX", "Vanguard Total Stock Market Index Fund", "USD"),
        ("VBTLX", "Vanguard Total Bond Market Index Fund", "USD"),
        ("VTIAX", "Vanguard Total International Stock Index Fund", "USD"),
        ("FBALX", "Fidelity Balanced Fund", "USD"),
        ("PIMIX", "PIMCO Income Fund Institutional", "USD"),
        ("VWELX", "Vanguard Wellington Fund", "USD"),
        ("TRBCX", "T. Rowe Price Blue Chip Growth Fund", "USD"),
        ("DODGX", "Dodge & Cox Stock Fund", "USD"),
    ];

    private static readonly (string Symbol, string Name)[] WarrantIndexData =
    [
        ("SPX", "S&P 500 Index"), ("NDX", "NASDAQ-100 Index"),
        ("DJI", "Dow Jones Industrial Average"), ("RUT", "Russell 2000 Index"),
        ("VIX", "CBOE Volatility Index"), ("FTSE", "FTSE 100 Index"),
        ("DAX", "DAX 40 Index"), ("NKY", "Nikkei 225 Index"),
        ("HSI", "Hang Seng Index"), ("STOXX50E", "Euro Stoxx 50 Index"),
    ];

    private static async Task<int> SeedInstrumentsAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Instruments.AnyAsync())
            return 0;

        var exchangeMap = await db.Exchanges.ToDictionaryAsync(e => e.Code, e => e.Id);
        var currencyMap = await db.Currencies.ToDictionaryAsync(c => c.Code, c => c.Id);
        var countryMap = await db.Countries.Where(c => c.IsActive).ToDictionaryAsync(c => c.Iso2, c => c.Id);

        Guid? ExId(string code) => exchangeMap.GetValueOrDefault(code);
        Guid? CurId(string code) => currencyMap.GetValueOrDefault(code);
        Guid? CntId(string iso2) => countryMap.GetValueOrDefault(iso2);

        var created = 0;

        // Stocks (~120)
        foreach (var s in StockData)
        {
            var rng = new Random(55_000 + created);
            db.Instruments.Add(new Instrument
            {
                Id = Guid.NewGuid(), Symbol = s.Symbol, Name = s.Name,
                ISIN = $"US{rng.Next(100000000, 999999999)}{rng.Next(0, 10)}",
                CUSIP = $"{rng.Next(100000, 999999)}{rng.Next(100, 999)}",
                Type = InstrumentType.Stock, AssetClass = AssetClass.Equities,
                Status = PickWeighted(rng, (InstrumentStatus.Active, 85), (InstrumentStatus.Inactive, 5),
                    (InstrumentStatus.Suspended, 5), (InstrumentStatus.Delisted, 5)),
                ExchangeId = ExId(s.Exchange), CurrencyId = CurId(s.Currency), CountryId = CntId(s.Country),
                Sector = s.Sector, LotSize = 1,
                TickSize = 0.01m, MarginRequirement = rng.Next(25, 51),
                IsMarginEligible = rng.Next(10) > 0,
                ListingDate = DateTime.UtcNow.AddDays(-rng.Next(365, 3650)),
                IssuerName = s.Name.Replace(" Inc.", "").Replace(" Corp.", "").Replace(" plc", ""),
                ExternalId = rng.Next(3) == 0 ? $"EXT-{rng.Next(10000, 99999)}" : null,
                CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(30, 365)), CreatedBy = "seed",
            });
            created++;
            if (created % 50 == 0) await db.SaveChangesAsync();
        }

        // ETFs (~30)
        foreach (var e in EtfData)
        {
            var rng = new Random(55_000 + created);
            db.Instruments.Add(new Instrument
            {
                Id = Guid.NewGuid(), Symbol = e.Symbol, Name = e.Name,
                CUSIP = $"{rng.Next(100000, 999999)}{rng.Next(100, 999)}",
                Type = InstrumentType.ETF, AssetClass = e.AC,
                Status = InstrumentStatus.Active,
                ExchangeId = ExId(e.Exchange), CurrencyId = CurId(e.Currency), CountryId = CntId("US"),
                LotSize = 1, TickSize = 0.01m, MarginRequirement = rng.Next(20, 40),
                IsMarginEligible = true,
                ListingDate = DateTime.UtcNow.AddDays(-rng.Next(365, 5000)),
                CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(30, 365)), CreatedBy = "seed",
            });
            created++;
            if (created % 50 == 0) await db.SaveChangesAsync();
        }

        // Bonds (~30)
        foreach (var b in BondData)
        {
            var rng = new Random(55_000 + created);
            db.Instruments.Add(new Instrument
            {
                Id = Guid.NewGuid(), Symbol = b.Symbol, Name = b.Name,
                CUSIP = $"{rng.Next(100000, 999999)}{rng.Next(100, 999)}",
                Type = InstrumentType.Bond, AssetClass = AssetClass.FixedIncome,
                Status = InstrumentStatus.Active,
                CurrencyId = CurId(b.Currency), CountryId = CntId("US"),
                LotSize = 1000, TickSize = 0.001m, MarginRequirement = rng.Next(5, 20),
                IsMarginEligible = true,
                ListingDate = DateTime.UtcNow.AddDays(-rng.Next(365, 3650)),
                ExpirationDate = DateTime.UtcNow.AddDays(rng.Next(365, 3650)),
                IssuerName = b.Name.Split(" Bond")[0].Split(" Corporate")[0],
                CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(30, 365)), CreatedBy = "seed",
            });
            created++;
            if (created % 50 == 0) await db.SaveChangesAsync();
        }

        // Options (~30)
        foreach (var o in OptionData)
        {
            var rng = new Random(55_000 + created);
            db.Instruments.Add(new Instrument
            {
                Id = Guid.NewGuid(), Symbol = o.Symbol, Name = o.Name,
                Type = InstrumentType.Option, AssetClass = AssetClass.Derivatives,
                Status = InstrumentStatus.Active,
                ExchangeId = ExId("NASDAQ"), CurrencyId = CurId("USD"), CountryId = CntId("US"),
                LotSize = 100, TickSize = 0.01m, MarginRequirement = rng.Next(20, 100),
                IsMarginEligible = true,
                ListingDate = DateTime.UtcNow.AddDays(-rng.Next(30, 180)),
                ExpirationDate = DateTime.UtcNow.AddDays(rng.Next(30, 365)),
                CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(10, 180)), CreatedBy = "seed",
            });
            created++;
            if (created % 50 == 0) await db.SaveChangesAsync();
        }

        // Futures (~30)
        foreach (var f in FutureData)
        {
            var rng = new Random(55_000 + created);
            db.Instruments.Add(new Instrument
            {
                Id = Guid.NewGuid(), Symbol = f.Symbol, Name = f.Name,
                Type = InstrumentType.Future, AssetClass = f.AC,
                Status = InstrumentStatus.Active,
                ExchangeId = ExId("NYSE"), CurrencyId = CurId(f.Currency),
                CountryId = CntId("US"),
                LotSize = f.Symbol is "ES" or "NQ" or "YM" or "RTY" ? 50 : 1000,
                TickSize = 0.25m, MarginRequirement = rng.Next(5, 15),
                IsMarginEligible = true,
                ListingDate = DateTime.UtcNow.AddDays(-rng.Next(30, 365)),
                ExpirationDate = DateTime.UtcNow.AddDays(rng.Next(30, 180)),
                CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(10, 180)), CreatedBy = "seed",
            });
            created++;
            if (created % 50 == 0) await db.SaveChangesAsync();
        }

        // Forex (~25)
        foreach (var fx in ForexData)
        {
            var rng = new Random(55_000 + created);
            db.Instruments.Add(new Instrument
            {
                Id = Guid.NewGuid(), Symbol = fx.Symbol, Name = fx.Name,
                Type = InstrumentType.Forex, AssetClass = AssetClass.ForeignExchange,
                Status = InstrumentStatus.Active,
                CurrencyId = CurId(fx.BaseCurrency),
                LotSize = 100_000, TickSize = 0.00001m,
                MarginRequirement = rng.Next(2, 5), IsMarginEligible = true,
                CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(30, 365)), CreatedBy = "seed",
            });
            created++;
            if (created % 50 == 0) await db.SaveChangesAsync();
        }

        // CFDs (~15)
        foreach (var c in CfdData)
        {
            var rng = new Random(55_000 + created);
            db.Instruments.Add(new Instrument
            {
                Id = Guid.NewGuid(), Symbol = c.Symbol, Name = c.Name,
                Type = InstrumentType.CFD, AssetClass = AssetClass.Derivatives,
                Status = InstrumentStatus.Active,
                CurrencyId = CurId("USD"),
                LotSize = 1, TickSize = 0.1m, MarginRequirement = rng.Next(5, 20),
                IsMarginEligible = true,
                CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(30, 365)), CreatedBy = "seed",
            });
            created++;
        }

        // Mutual Funds (~10)
        foreach (var mf in MutualFundData)
        {
            var rng = new Random(55_000 + created);
            db.Instruments.Add(new Instrument
            {
                Id = Guid.NewGuid(), Symbol = mf.Symbol, Name = mf.Name,
                Type = InstrumentType.MutualFund, AssetClass = AssetClass.Funds,
                Status = InstrumentStatus.Active,
                CurrencyId = CurId(mf.Currency), CountryId = CntId("US"),
                LotSize = 1, IsMarginEligible = false,
                ListingDate = DateTime.UtcNow.AddDays(-rng.Next(730, 7300)),
                CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(30, 365)), CreatedBy = "seed",
            });
            created++;
        }

        // Warrants/Index (~10)
        foreach (var wi in WarrantIndexData)
        {
            var rng = new Random(55_000 + created);
            db.Instruments.Add(new Instrument
            {
                Id = Guid.NewGuid(), Symbol = wi.Symbol, Name = wi.Name,
                Type = InstrumentType.Index, AssetClass = AssetClass.Equities,
                Status = InstrumentStatus.Active,
                CurrencyId = CurId("USD"),
                LotSize = 1, IsMarginEligible = false,
                CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(30, 365)), CreatedBy = "seed",
            });
            created++;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Demo seed: created {Count} instruments", created);
        return created;
    }

    // ── Orders ─────────────────────────────────────────────────────

    private const int TradeOrderCount = 1200;
    private const int NonTradeOrderCount = 100;

    private static async Task<int> SeedOrdersAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Orders.AnyAsync())
            return 0;

        var accountIds = await db.Accounts.Where(a => a.Status == AccountStatus.Active)
            .Select(a => a.Id).ToArrayAsync();
        var instrumentIds = await db.Instruments.Where(i => i.Status == InstrumentStatus.Active)
            .Select(i => i.Id).ToArrayAsync();
        var currencyIds = await db.Currencies.Where(c => c.IsActive)
            .Select(c => c.Id).ToArrayAsync();

        if (accountIds.Length == 0 || instrumentIds.Length == 0 || currencyIds.Length == 0)
        {
            logger.LogWarning("Demo seed: skipping orders — no accounts, instruments, or currencies found");
            return 0;
        }

        var created = 0;

        // ── Trade Orders ──
        for (var i = 0; i < TradeOrderCount; i++)
        {
            var rng = new Random(110_000 + i);
            var orderId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-rng.Next(1, 365)).AddMinutes(-rng.Next(0, 1440));

            var status = PickWeighted(rng,
                (OrderStatus.New, 10),
                (OrderStatus.PendingApproval, 5),
                (OrderStatus.Approved, 5),
                (OrderStatus.Rejected, 5),
                (OrderStatus.InProgress, 10),
                (OrderStatus.PartiallyFilled, 10),
                (OrderStatus.Filled, 25),
                (OrderStatus.Completed, 15),
                (OrderStatus.Cancelled, 10),
                (OrderStatus.Failed, 5));

            var side = PickWeighted(rng,
                (TradeSide.Buy, 40),
                (TradeSide.Sell, 40),
                (TradeSide.ShortSell, 10),
                (TradeSide.BuyToCover, 10));

            var orderType = PickWeighted(rng,
                (TradeOrderType.Market, 40),
                (TradeOrderType.Limit, 35),
                (TradeOrderType.Stop, 15),
                (TradeOrderType.StopLimit, 10));

            var timeInForce = PickWeighted(rng,
                (TimeInForce.Day, 40),
                (TimeInForce.GTC, 30),
                (TimeInForce.IOC, 10),
                (TimeInForce.FOK, 10),
                (TimeInForce.GTD, 10));

            var quantity = Math.Round((decimal)(rng.Next(1, 500) * rng.Next(1, 10)), 2);
            var price = orderType is TradeOrderType.Limit or TradeOrderType.StopLimit
                ? Math.Round((decimal)(rng.NextDouble() * 500 + 1), 2)
                : (decimal?)null;
            var stopPrice = orderType is TradeOrderType.Stop or TradeOrderType.StopLimit
                ? Math.Round((decimal)(rng.NextDouble() * 500 + 1), 2)
                : (decimal?)null;

            var isFilled = status is OrderStatus.Filled or OrderStatus.Completed;
            var isPartial = status == OrderStatus.PartiallyFilled;
            var executedQty = isFilled ? quantity
                : isPartial ? Math.Round(quantity * (decimal)(rng.NextDouble() * 0.8 + 0.1), 2)
                : 0m;
            var avgPrice = executedQty > 0
                ? Math.Round((decimal)(rng.NextDouble() * 500 + 1), 2)
                : (decimal?)null;
            var commission = executedQty > 0
                ? Math.Round((decimal)(rng.NextDouble() * 20 + 0.5), 2)
                : (decimal?)null;
            var executedAt = executedQty > 0
                ? createdAt.AddMinutes(rng.Next(1, 480))
                : (DateTime?)null;

            db.Orders.Add(new Order
            {
                Id = orderId,
                AccountId = accountIds[rng.Next(accountIds.Length)],
                OrderNumber = $"TO-{createdAt:yyyyMMdd}-{orderId.ToString("N")[..8].ToUpper()}",
                Category = OrderCategory.Trade,
                Status = status,
                OrderDate = createdAt.Date,
                Comment = rng.Next(5) == 0 ? "Demo trade order" : null,
                ExternalId = rng.Next(4) == 0 ? $"EXT-TO-{rng.Next(10000, 99999)}" : null,
                CreatedAt = createdAt,
                CreatedBy = "seed",
            });

            db.TradeOrders.Add(new TradeOrder
            {
                OrderId = orderId,
                InstrumentId = instrumentIds[rng.Next(instrumentIds.Length)],
                Side = side,
                OrderType = orderType,
                TimeInForce = timeInForce,
                Quantity = quantity,
                Price = price,
                StopPrice = stopPrice,
                ExecutedQuantity = executedQty,
                AveragePrice = avgPrice,
                Commission = commission,
                ExecutedAt = executedAt,
                ExpirationDate = timeInForce == TimeInForce.GTD
                    ? createdAt.AddDays(rng.Next(1, 30))
                    : null,
            });

            created++;
            if (i % 50 == 49)
                await db.SaveChangesAsync();
        }

        await db.SaveChangesAsync();

        // ── Non-Trade Orders ──
        for (var i = 0; i < NonTradeOrderCount; i++)
        {
            var rng = new Random(120_000 + i);
            var orderId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-rng.Next(1, 365)).AddMinutes(-rng.Next(0, 1440));

            var status = PickWeighted(rng,
                (OrderStatus.New, 10),
                (OrderStatus.PendingApproval, 10),
                (OrderStatus.Approved, 10),
                (OrderStatus.Completed, 40),
                (OrderStatus.Rejected, 10),
                (OrderStatus.Cancelled, 10),
                (OrderStatus.Failed, 10));

            var nonTradeType = PickWeighted(rng,
                (NonTradeOrderType.Deposit, 25),
                (NonTradeOrderType.Withdrawal, 20),
                (NonTradeOrderType.Dividend, 15),
                (NonTradeOrderType.Fee, 10),
                (NonTradeOrderType.Interest, 10),
                (NonTradeOrderType.Transfer, 10),
                (NonTradeOrderType.CorporateAction, 5),
                (NonTradeOrderType.Adjustment, 5));

            var amount = nonTradeType switch
            {
                NonTradeOrderType.Deposit => Math.Round((decimal)(rng.NextDouble() * 50000 + 100), 2),
                NonTradeOrderType.Withdrawal => -Math.Round((decimal)(rng.NextDouble() * 20000 + 100), 2),
                NonTradeOrderType.Dividend => Math.Round((decimal)(rng.NextDouble() * 500 + 1), 2),
                NonTradeOrderType.Fee => -Math.Round((decimal)(rng.NextDouble() * 100 + 1), 2),
                NonTradeOrderType.Interest => Math.Round((decimal)(rng.NextDouble() * 200 + 0.5), 2),
                _ => Math.Round((decimal)(rng.NextDouble() * 10000 + 10), 2),
            };

            var needsInstrument = nonTradeType is NonTradeOrderType.Dividend or NonTradeOrderType.CorporateAction;
            var processedAt = status == OrderStatus.Completed
                ? createdAt.AddHours(rng.Next(1, 72))
                : (DateTime?)null;

            db.Orders.Add(new Order
            {
                Id = orderId,
                AccountId = accountIds[rng.Next(accountIds.Length)],
                OrderNumber = $"NTO-{createdAt:yyyyMMdd}-{orderId.ToString("N")[..8].ToUpper()}",
                Category = OrderCategory.NonTrade,
                Status = status,
                OrderDate = createdAt.Date,
                Comment = rng.Next(5) == 0 ? "Demo non-trade order" : null,
                ExternalId = rng.Next(4) == 0 ? $"EXT-NTO-{rng.Next(10000, 99999)}" : null,
                CreatedAt = createdAt,
                CreatedBy = "seed",
            });

            db.NonTradeOrders.Add(new NonTradeOrder
            {
                OrderId = orderId,
                NonTradeType = nonTradeType,
                Amount = amount,
                CurrencyId = currencyIds[rng.Next(currencyIds.Length)],
                InstrumentId = needsInstrument ? instrumentIds[rng.Next(instrumentIds.Length)] : null,
                ReferenceNumber = rng.Next(3) == 0 ? $"REF-{rng.Next(100000, 999999)}" : null,
                Description = rng.Next(4) == 0 ? $"Demo {nonTradeType} order" : null,
                ProcessedAt = processedAt,
            });

            created++;
            if (i % 50 == 49)
                await db.SaveChangesAsync();
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Demo seed: created {Count} orders ({Trade} trade, {NonTrade} non-trade)",
            created, TradeOrderCount, NonTradeOrderCount);
        return created;
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static T PickRandom<T>(Random rng, params T[] options) =>
        options[rng.Next(options.Length)];

    private static T PickWeighted<T>(Random rng, params (T Value, int Weight)[] options)
    {
        var total = options.Sum(o => o.Weight);
        var roll = rng.Next(total);
        var cumulative = 0;
        foreach (var (value, weight) in options)
        {
            cumulative += weight;
            if (roll < cumulative) return value;
        }
        return options[^1].Value;
    }
}
