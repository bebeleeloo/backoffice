using Broker.Auth.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Broker.Auth.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(AuthDbContext db, IConfiguration config, ILogger logger)
    {
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

        // Ensure admin role has all permissions
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

        // Demo data
        var env = config["ASPNETCORE_ENVIRONMENT"] ?? "";
        var seedDemo = string.Equals(config["SEED_DEMO_DATA"], "true", StringComparison.OrdinalIgnoreCase);
        var isDevelopment = string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);

        if (seedDemo || isDevelopment)
        {
            await SeedDemoData.SeedAsync(db, config, logger);
        }
    }
}
