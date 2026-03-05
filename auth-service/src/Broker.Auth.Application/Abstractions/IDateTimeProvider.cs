namespace Broker.Auth.Application.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
