using Broker.Auth.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Broker.Auth.Infrastructure.Persistence;

public static class SeedDemoData
{
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

    public static async Task SeedAsync(AuthDbContext db, IConfiguration config, ILogger logger)
    {
        var password = config["DEFAULT_DEMO_PASSWORD"] ?? config["ADMIN_PASSWORD"] ?? "Admin123!";

        var existingUsernames = (await db.Users.Select(u => u.Username).ToListAsync()).ToHashSet();
        var allPermissions = await db.Permissions.ToDictionaryAsync(p => p.Code);
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

        foreach (var (roleName, (_, permCodes)) in RoleDefinitions)
        {
            var role = roles[roleName];
            var existing = (await db.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync()).ToHashSet();

            foreach (var code in permCodes)
            {
                if (!allPermissions.TryGetValue(code, out var perm)) continue;
                if (existing.Contains(perm.Id)) continue;
                db.RolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(), RoleId = role.Id, PermissionId = perm.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        await db.SaveChangesAsync();

        var hasher = new PasswordHasher<User>();
        var created = 0;
        foreach (var (username, email, fullName, roleName) in DemoUsers)
        {
            if (existingUsernames.Contains(username)) continue;
            var user = new User
            {
                Id = Guid.NewGuid(), Username = username, Email = email,
                FullName = fullName, IsActive = true, CreatedAt = DateTime.UtcNow
            };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);

            if (roles.TryGetValue(roleName, out var role))
            {
                db.UserRoles.Add(new UserRole
                {
                    Id = Guid.NewGuid(), UserId = user.Id, RoleId = role.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
            created++;
        }
        await db.SaveChangesAsync();

        if (created > 0)
            logger.LogInformation("Seeded {Count} demo users", created);

        await SeedUserPhotosAsync(db, logger);
    }

    private static readonly Dictionary<string, string> UserPhotoUrls = new()
    {
        ["admin"]     = "https://randomuser.me/api/portraits/men/32.jpg",
        ["jdoe"]      = "https://randomuser.me/api/portraits/men/75.jpg",
        ["asmith"]    = "https://randomuser.me/api/portraits/women/44.jpg",
        ["bwilson"]   = "https://randomuser.me/api/portraits/men/22.jpg",
        ["cjohnson"]  = "https://randomuser.me/api/portraits/women/68.jpg",
        ["dlee"]      = "https://randomuser.me/api/portraits/men/45.jpg",
        ["ebrown"]    = "https://randomuser.me/api/portraits/women/26.jpg",
        ["fgarcia"]   = "https://randomuser.me/api/portraits/men/67.jpg",
        ["gmartinez"] = "https://randomuser.me/api/portraits/women/52.jpg",
        ["hchen"]     = "https://randomuser.me/api/portraits/men/91.jpg",
        ["itaylor"]   = "https://randomuser.me/api/portraits/women/89.jpg",
    };

    private static async Task SeedUserPhotosAsync(AuthDbContext db, ILogger logger)
    {
        var users = await db.Users
            .Where(u => u.Photo == null || u.Photo.Length < 1024)
            .ToListAsync();

        if (users.Count == 0) return;

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var downloaded = 0;

        foreach (var user in users)
        {
            if (!UserPhotoUrls.TryGetValue(user.Username, out var url)) continue;
            try
            {
                var bytes = await http.GetByteArrayAsync(url);
                user.Photo = bytes;
                user.PhotoContentType = "image/jpeg";
                downloaded++;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Failed to download photo for {Username}: {Error}", user.Username, ex.Message);
            }
        }

        if (downloaded > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded photos for {Count} users", downloaded);
        }
    }
}
