using Broker.Backoffice.Domain.Instruments;

namespace Broker.Backoffice.Domain.Orders;

public sealed class TradeOrder
{
    public Guid Id { get; init; }

    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid InstrumentId { get; set; }
    public Instrument? Instrument { get; set; }

    public TradeSide Side { get; set; }
    public TradeOrderType OrderType { get; set; }
    public TimeInForce TimeInForce { get; set; }

    public decimal Quantity { get; set; }
    public decimal? Price { get; set; }
    public decimal? StopPrice { get; set; }

    public decimal ExecutedQuantity { get; set; }
    public decimal? AveragePrice { get; set; }

    public decimal? Commission { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public DateTime? ExpirationDate { get; set; }
}
