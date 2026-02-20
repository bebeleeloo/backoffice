using Broker.Backoffice.Domain.Identity;

namespace Broker.Backoffice.Application.Abstractions;

public record TokenPair(string AccessToken, string RefreshToken, DateTime AccessTokenExpires);

public interface IJwtTokenService
{
    TokenPair GenerateTokens(User user, IReadOnlyList<string> permissions);
    string HashToken(string token);
}
