using Broker.Backoffice.Domain.Instruments;
using Broker.Backoffice.Domain.Orders;

namespace Broker.Backoffice.Domain.Transactions;

public sealed class TradeTransaction
{
    public Guid Id { get; init; }

    public Guid TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    public Guid InstrumentId { get; set; }
    public Instrument? Instrument { get; set; }

    public TradeSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal? Commission { get; set; }
    public DateTime? SettlementDate { get; set; }
    public string? Venue { get; set; }
}
