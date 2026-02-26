using Broker.Backoffice.Domain.Instruments;

namespace Broker.Backoffice.Domain.Orders;

public sealed class NonTradeOrder
{
    public Guid Id { get; init; }

    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public NonTradeOrderType NonTradeType { get; set; }

    public decimal Amount { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public Guid? InstrumentId { get; set; }
    public Instrument? Instrument { get; set; }

    public string? ReferenceNumber { get; set; }
    public string? Description { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
