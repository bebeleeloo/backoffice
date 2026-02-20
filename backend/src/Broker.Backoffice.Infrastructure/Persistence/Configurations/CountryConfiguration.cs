using Broker.Backoffice.Domain.Countries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Broker.Backoffice.Infrastructure.Persistence.Configurations;

public sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Iso2).HasMaxLength(2).IsRequired();
        b.HasIndex(e => e.Iso2).IsUnique();
        b.Property(e => e.Iso3).HasMaxLength(3);
        b.HasIndex(e => e.Iso3).IsUnique().HasFilter("[Iso3] IS NOT NULL");
        b.Property(e => e.Name).HasMaxLength(100).IsRequired();
        b.Property(e => e.FlagEmoji).HasMaxLength(8).IsRequired();
        b.Property(e => e.IsActive).HasDefaultValue(true);
    }
}
