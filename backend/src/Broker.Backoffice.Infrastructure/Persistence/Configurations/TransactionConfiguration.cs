using Broker.Backoffice.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Broker.Backoffice.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> b)
    {
        b.HasKey(e => e.Id);

        b.Property(e => e.TransactionNumber).HasMaxLength(50).IsRequired();
        b.HasIndex(e => e.TransactionNumber).IsUnique();

        b.Property(e => e.Category).IsRequired();
        b.Property(e => e.Status).IsRequired();
        b.Property(e => e.TransactionDate).IsRequired();

        b.HasOne(e => e.Order).WithMany()
            .HasForeignKey(e => e.OrderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        b.Property(e => e.Comment).HasMaxLength(500);
        b.Property(e => e.ExternalId).HasMaxLength(64);

        b.Property(e => e.RowVersion).IsRowVersion();
        b.Property(e => e.CreatedBy).HasMaxLength(100);
        b.Property(e => e.UpdatedBy).HasMaxLength(100);

        b.HasIndex(e => new { e.Category, e.Status });
        b.HasIndex(e => e.OrderId);
        b.HasIndex(e => e.TransactionDate);
    }
}

public sealed class TradeTransactionConfiguration : IEntityTypeConfiguration<TradeTransaction>
{
    public void Configure(EntityTypeBuilder<TradeTransaction> b)
    {
        b.HasKey(e => e.Id);

        b.HasOne(e => e.Transaction).WithMany()
            .HasForeignKey(e => e.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(e => e.TransactionId).IsUnique();

        b.HasOne(e => e.Instrument).WithMany()
            .HasForeignKey(e => e.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Property(e => e.Side).IsRequired();

        b.Property(e => e.Quantity).HasPrecision(18, 6).IsRequired();
        b.Property(e => e.Price).HasPrecision(18, 6).IsRequired();
        b.Property(e => e.Commission).HasPrecision(18, 6);

        b.Property(e => e.Venue).HasMaxLength(100);

        b.HasIndex(e => e.Side);
    }
}

public sealed class NonTradeTransactionConfiguration : IEntityTypeConfiguration<NonTradeTransaction>
{
    public void Configure(EntityTypeBuilder<NonTradeTransaction> b)
    {
        b.HasKey(e => e.Id);

        b.HasOne(e => e.Transaction).WithMany()
            .HasForeignKey(e => e.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(e => e.TransactionId).IsUnique();

        b.Property(e => e.Amount).HasPrecision(18, 6).IsRequired();

        b.HasOne(e => e.Currency).WithMany()
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(e => e.Instrument).WithMany()
            .HasForeignKey(e => e.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Property(e => e.ReferenceNumber).HasMaxLength(100);
        b.Property(e => e.Description).HasMaxLength(500);
    }
}
