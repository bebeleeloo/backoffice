using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Broker.Auth.Application.Abstractions;
using Broker.Auth.Domain.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Broker.Auth.Infrastructure.Services;

public sealed class JwtTokenService(IConfiguration config) : IJwtTokenService
{
    public TokenPair GenerateTokens(User user, IReadOnlyList<string> permissions)
    {
        var secret = config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured");
        if (secret.Length < 32)
            throw new InvalidOperationException("Jwt:Secret must be at least 32 characters");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresMin = int.Parse(config["Jwt:AccessTokenExpirationMinutes"] ?? "30");
        var expires = DateTime.UtcNow.AddMinutes(expiresMin);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("full_name", user.FullName ?? user.Username)
        };

        foreach (var p in permissions)
            claims.Add(new Claim("permission", p));

        foreach (var ur in user.UserRoles)
            claims.Add(new Claim(ClaimTypes.Role, ur.Role.Name));

        var issuer = config["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is not configured");
        var audience = config["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is not configured");

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new TokenPair(accessToken, refreshToken, expires);
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
