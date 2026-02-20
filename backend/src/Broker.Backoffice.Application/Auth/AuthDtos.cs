namespace Broker.Backoffice.Application.Auth;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);

public sealed record UserProfileResponse(
    Guid Id,
    string Username,
    string Email,
    string? FullName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<DataScopeDto> Scopes);

public sealed record DataScopeDto(string ScopeType, string ScopeValue);
