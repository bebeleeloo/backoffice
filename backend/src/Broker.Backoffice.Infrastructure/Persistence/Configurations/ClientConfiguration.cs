using Broker.Backoffice.Domain.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Broker.Backoffice.Infrastructure.Persistence.Configurations;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> b)
    {
        b.HasKey(e => e.Id);

        b.Property(e => e.ClientType).IsRequired();
        b.Property(e => e.Status).IsRequired();
        b.Property(e => e.KycStatus).IsRequired();

        b.Property(e => e.ExternalId).HasMaxLength(64);
        b.Property(e => e.Email).HasMaxLength(200).IsRequired();
        b.HasIndex(e => e.Email).IsUnique();
        b.Property(e => e.Phone).HasMaxLength(32);
        b.Property(e => e.PreferredLanguage).HasMaxLength(10);
        b.Property(e => e.TimeZone).HasMaxLength(64);

        // Country FKs
        b.HasOne(e => e.ResidenceCountry).WithMany()
            .HasForeignKey(e => e.ResidenceCountryId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.CitizenshipCountry).WithMany()
            .HasForeignKey(e => e.CitizenshipCountryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Property(e => e.FirstName).HasMaxLength(100);
        b.Property(e => e.LastName).HasMaxLength(100);
        b.Property(e => e.MiddleName).HasMaxLength(100);

        // Identity documents
        b.Property(e => e.Ssn).HasMaxLength(20);
        b.Property(e => e.PassportNumber).HasMaxLength(30);
        b.Property(e => e.DriverLicenseNumber).HasMaxLength(30);

        b.Property(e => e.CompanyName).HasMaxLength(200);
        b.Property(e => e.RegistrationNumber).HasMaxLength(64);
        b.Property(e => e.TaxId).HasMaxLength(64);

        b.Property(e => e.RowVersion).IsRowVersion();
        b.Property(e => e.CreatedBy).HasMaxLength(100);
        b.Property(e => e.UpdatedBy).HasMaxLength(100);

        b.HasIndex(e => new { e.ClientType, e.Status });
    }
}

public sealed class ClientAddressConfiguration : IEntityTypeConfiguration<ClientAddress>
{
    public void Configure(EntityTypeBuilder<ClientAddress> b)
    {
        b.HasKey(e => e.Id);

        b.Property(e => e.Type).IsRequired();
        b.Property(e => e.Line1).HasMaxLength(200).IsRequired();
        b.Property(e => e.Line2).HasMaxLength(200);
        b.Property(e => e.City).HasMaxLength(100).IsRequired();
        b.Property(e => e.State).HasMaxLength(100);
        b.Property(e => e.PostalCode).HasMaxLength(20);

        b.HasOne(e => e.Country).WithMany()
            .HasForeignKey(e => e.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(e => e.ClientId);
        b.HasOne(e => e.Client).WithMany(c => c.Addresses)
            .HasForeignKey(e => e.ClientId).OnDelete(DeleteBehavior.Cascade);
    }
}
