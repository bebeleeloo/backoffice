namespace Broker.Auth.Application.Abstractions;

public interface ICorrelationIdAccessor
{
    string CorrelationId { get; set; }
}
