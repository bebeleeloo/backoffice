using Broker.Backoffice.Domain.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Broker.Backoffice.Infrastructure.Persistence.Configurations;

public sealed class InvestmentProfileConfiguration : IEntityTypeConfiguration<InvestmentProfile>
{
    public void Configure(EntityTypeBuilder<InvestmentProfile> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Notes).HasMaxLength(2000);

        b.HasIndex(e => e.ClientId).IsUnique();
        b.HasOne(e => e.Client)
            .WithOne(c => c.InvestmentProfile)
            .HasForeignKey<InvestmentProfile>(e => e.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
