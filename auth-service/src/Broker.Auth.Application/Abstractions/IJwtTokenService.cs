using Broker.Auth.Domain.Identity;

namespace Broker.Auth.Application.Abstractions;

public record TokenPair(string AccessToken, string RefreshToken, DateTime AccessTokenExpires);

public interface IJwtTokenService
{
    TokenPair GenerateTokens(User user, IReadOnlyList<string> permissions);
    string HashToken(string token);
}
