using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Broker.Backoffice.Infrastructure.Auth;

public sealed class BasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Basic";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return AuthenticateResult.NoResult();

        var header = authHeader.ToString();
        if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        string username, password;
        try
        {
            var decoded = Encoding.UTF8.GetString(
                Convert.FromBase64String(header["Basic ".Length..]));
            var sep = decoded.IndexOf(':');
            if (sep <= 0)
                return AuthenticateResult.Fail("Invalid Basic credential format");
            username = decoded[..sep];
            password = decoded[(sep + 1)..];
        }
        catch (FormatException)
        {
            return AuthenticateResult.Fail("Invalid Base64 encoding");
        }

        var cacheKey = $"basic-auth:{username}:{password.GetHashCode()}";

        if (cache.TryGetValue(cacheKey, out ClaimsPrincipal? cachedPrincipal) && cachedPrincipal is not null)
            return AuthenticateResult.Success(new AuthenticationTicket(cachedPrincipal, SchemeName));

        try
        {
            var httpClient = httpClientFactory.CreateClient("AuthService");
            var response = await httpClient.PostAsJsonAsync("/api/v1/auth/login",
                new { username, password });

            if (!response.IsSuccessStatusCode)
                return AuthenticateResult.Fail("Invalid credentials");

            var loginResult = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (loginResult?.AccessToken is null)
                return AuthenticateResult.Fail("No access token in response");

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(loginResult.AccessToken);

            var identity = new ClaimsIdentity(jwt.Claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);

            cache.Set(cacheKey, principal, TimeSpan.FromMinutes(5));

            return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail($"Authentication failed: {ex.Message}");
        }
    }

    private sealed record LoginResponse(string? AccessToken);
}
