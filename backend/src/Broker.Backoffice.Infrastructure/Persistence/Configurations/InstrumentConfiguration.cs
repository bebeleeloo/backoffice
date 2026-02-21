using Broker.Backoffice.Domain.Instruments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Broker.Backoffice.Infrastructure.Persistence.Configurations;

public sealed class InstrumentConfiguration : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> b)
    {
        b.HasKey(e => e.Id);

        b.Property(e => e.Symbol).HasMaxLength(20).IsRequired();
        b.HasIndex(e => e.Symbol).IsUnique();

        b.Property(e => e.Name).HasMaxLength(255).IsRequired();
        b.Property(e => e.ISIN).HasMaxLength(12);
        b.Property(e => e.CUSIP).HasMaxLength(9);

        b.Property(e => e.Type).IsRequired();
        b.Property(e => e.AssetClass).IsRequired();
        b.Property(e => e.Status).IsRequired();

        b.HasOne(e => e.Exchange).WithMany()
            .HasForeignKey(e => e.ExchangeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(e => e.Currency).WithMany()
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(e => e.Country).WithMany()
            .HasForeignKey(e => e.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Property(e => e.LotSize).HasDefaultValue(1);
        b.Property(e => e.TickSize).HasPrecision(10, 6);
        b.Property(e => e.MarginRequirement).HasPrecision(5, 2);
        b.Property(e => e.IsMarginEligible).HasDefaultValue(true);

        b.Property(e => e.IssuerName).HasMaxLength(255);
        b.Property(e => e.Description).HasMaxLength(1000);
        b.Property(e => e.ExternalId).HasMaxLength(64);

        b.Property(e => e.RowVersion).IsRowVersion();
        b.Property(e => e.CreatedBy).HasMaxLength(100);
        b.Property(e => e.UpdatedBy).HasMaxLength(100);

        b.HasIndex(e => new { e.Type, e.AssetClass, e.Status });
    }
}

public sealed class ExchangeConfiguration : IEntityTypeConfiguration<Exchange>
{
    public void Configure(EntityTypeBuilder<Exchange> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Code).HasMaxLength(20).IsRequired();
        b.HasIndex(e => e.Code).IsUnique();
        b.Property(e => e.Name).HasMaxLength(200).IsRequired();
        b.HasOne(e => e.Country).WithMany()
            .HasForeignKey(e => e.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Property(e => e.IsActive).HasDefaultValue(true);
    }
}

public sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Code).HasMaxLength(3).IsRequired();
        b.HasIndex(e => e.Code).IsUnique();
        b.Property(e => e.Name).HasMaxLength(100).IsRequired();
        b.Property(e => e.Symbol).HasMaxLength(5);
        b.Property(e => e.IsActive).HasDefaultValue(true);
    }
}
