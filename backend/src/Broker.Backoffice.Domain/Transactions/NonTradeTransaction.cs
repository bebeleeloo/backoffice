using Broker.Backoffice.Domain.Instruments;

namespace Broker.Backoffice.Domain.Transactions;

public sealed class NonTradeTransaction
{
    public Guid Id { get; init; }

    public Guid TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    public decimal Amount { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public Guid? InstrumentId { get; set; }
    public Instrument? Instrument { get; set; }

    public string? ReferenceNumber { get; set; }
    public string? Description { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
