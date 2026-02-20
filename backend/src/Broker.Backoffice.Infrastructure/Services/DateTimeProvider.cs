using Broker.Backoffice.Application.Abstractions;

namespace Broker.Backoffice.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
