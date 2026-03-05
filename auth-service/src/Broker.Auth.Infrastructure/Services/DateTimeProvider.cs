using Broker.Auth.Application.Abstractions;

namespace Broker.Auth.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
