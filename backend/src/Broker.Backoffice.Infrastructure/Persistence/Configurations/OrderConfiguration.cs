using Broker.Backoffice.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Broker.Backoffice.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.HasKey(e => e.Id);

        b.Property(e => e.OrderNumber).HasMaxLength(50).IsRequired();
        b.HasIndex(e => e.OrderNumber).IsUnique();

        b.Property(e => e.Category).IsRequired();
        b.Property(e => e.Status).IsRequired();
        b.Property(e => e.OrderDate).IsRequired();

        b.HasOne(e => e.Account).WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Property(e => e.Comment).HasMaxLength(500);
        b.Property(e => e.ExternalId).HasMaxLength(64);

        b.Property(e => e.RowVersion).IsRowVersion();
        b.Property(e => e.CreatedBy).HasMaxLength(100);
        b.Property(e => e.UpdatedBy).HasMaxLength(100);

        b.HasIndex(e => new { e.Category, e.Status });
        b.HasIndex(e => e.AccountId);
    }
}

public sealed class TradeOrderConfiguration : IEntityTypeConfiguration<TradeOrder>
{
    public void Configure(EntityTypeBuilder<TradeOrder> b)
    {
        b.HasKey(e => e.Id);

        b.HasOne(e => e.Order).WithMany()
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(e => e.OrderId).IsUnique();

        b.HasOne(e => e.Instrument).WithMany()
            .HasForeignKey(e => e.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Property(e => e.Side).IsRequired();
        b.Property(e => e.OrderType).IsRequired();
        b.Property(e => e.TimeInForce).IsRequired();

        b.Property(e => e.Quantity).HasPrecision(18, 6).IsRequired();
        b.Property(e => e.Price).HasPrecision(18, 6);
        b.Property(e => e.StopPrice).HasPrecision(18, 6);
        b.Property(e => e.ExecutedQuantity).HasPrecision(18, 6).HasDefaultValue(0m);
        b.Property(e => e.AveragePrice).HasPrecision(18, 6);
        b.Property(e => e.Commission).HasPrecision(18, 6);

        b.HasIndex(e => new { e.Side, e.OrderType });
    }
}

public sealed class NonTradeOrderConfiguration : IEntityTypeConfiguration<NonTradeOrder>
{
    public void Configure(EntityTypeBuilder<NonTradeOrder> b)
    {
        b.HasKey(e => e.Id);

        b.HasOne(e => e.Order).WithMany()
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(e => e.OrderId).IsUnique();

        b.Property(e => e.NonTradeType).IsRequired();
        b.Property(e => e.Amount).HasPrecision(18, 6).IsRequired();

        b.HasOne(e => e.Currency).WithMany()
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(e => e.Instrument).WithMany()
            .HasForeignKey(e => e.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Property(e => e.ReferenceNumber).HasMaxLength(100);
        b.Property(e => e.Description).HasMaxLength(500);

        b.HasIndex(e => e.NonTradeType);
    }
}
