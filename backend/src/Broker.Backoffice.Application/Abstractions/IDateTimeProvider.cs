namespace Broker.Backoffice.Application.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
