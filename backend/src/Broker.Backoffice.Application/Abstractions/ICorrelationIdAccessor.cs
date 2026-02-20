namespace Broker.Backoffice.Application.Abstractions;

public interface ICorrelationIdAccessor
{
    string CorrelationId { get; set; }
}
