using Broker.Backoffice.Domain.Accounts;
using Broker.Backoffice.Domain.Clients;
using Broker.Backoffice.Domain.Countries;
using Broker.Backoffice.Domain.Identity;
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

        await tx.CommitAsync();

        if (usersCreated + clientsCreated + accountsCreated > 0)
            logger.LogInformation("Demo seed complete: {Users} users, {Clients} clients, {Accounts} accounts created",
                usersCreated, clientsCreated, accountsCreated);
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
        ["Manager"] = ("Manage clients, accounts, and view everything", new[]
        {
            Permissions.UsersRead, Permissions.RolesRead, Permissions.PermissionsRead,
            Permissions.AuditRead,
            Permissions.ClientsRead, Permissions.ClientsCreate, Permissions.ClientsUpdate, Permissions.ClientsDelete,
            Permissions.AccountsRead, Permissions.AccountsCreate, Permissions.AccountsUpdate, Permissions.AccountsDelete,
        }),
        ["Viewer"] = ("Read-only access", new[]
        {
            Permissions.UsersRead, Permissions.RolesRead, Permissions.PermissionsRead,
            Permissions.AuditRead, Permissions.ClientsRead, Permissions.AccountsRead,
        }),
        ["Operator"] = ("Client and account operations", new[]
        {
            Permissions.ClientsRead, Permissions.ClientsCreate, Permissions.ClientsUpdate,
            Permissions.AccountsRead, Permissions.AccountsCreate, Permissions.AccountsUpdate,
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
