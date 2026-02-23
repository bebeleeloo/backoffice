namespace Broker.Backoffice.Application.Abstractions;

public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }
    string? FullName { get; }
    bool IsAuthenticated { get; }
}
