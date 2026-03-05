using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Broker.Backoffice.Tests.Integration;

public static class TestJwtTokenHelper
{
    // Must match appsettings.Development.json
    private const string Secret = "this-is-a-development-secret-key-min-32-chars!!";
    private const string Issuer = "BrokerBackoffice";
    private const string Audience = "BrokerBackoffice";

    public static string GenerateToken(
        string userId = "00000000-0000-0000-0000-000000000001",
        string username = "admin",
        string fullName = "System Administrator",
        IEnumerable<string>? permissions = null)
    {
        var claims = new List<Claim>
        {
            new("sub", userId),
            new("username", username),
            new("fullname", fullName),
        };

        if (permissions != null)
        {
            foreach (var p in permissions)
                claims.Add(new Claim("permission", p));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateAdminToken()
    {
        // All 31 permissions
        var allPermissions = new[]
        {
            "users.read", "users.create", "users.update", "users.delete",
            "roles.read", "roles.create", "roles.update", "roles.delete",
            "permissions.read",
            "audit.read",
            "clients.read", "clients.create", "clients.update", "clients.delete",
            "accounts.read", "accounts.create", "accounts.update", "accounts.delete",
            "instruments.read", "instruments.create", "instruments.update", "instruments.delete",
            "orders.read", "orders.create", "orders.update", "orders.delete",
            "transactions.read", "transactions.create", "transactions.update", "transactions.delete",
            "settings.manage"
        };

        return GenerateToken(permissions: allPermissions);
    }
}
