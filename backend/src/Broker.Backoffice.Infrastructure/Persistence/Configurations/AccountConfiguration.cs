using Broker.Backoffice.Domain.Accounts;
using Broker.Backoffice.Domain.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Broker.Backoffice.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> b)
    {
        b.HasKey(e => e.Id);

        b.Property(e => e.Number).HasMaxLength(50).IsRequired();
        b.HasIndex(e => e.Number).IsUnique();

        b.Property(e => e.Status).IsRequired();
        b.Property(e => e.AccountType).IsRequired();
        b.Property(e => e.MarginType).IsRequired();
        b.Property(e => e.OptionLevel).IsRequired();
        b.Property(e => e.Tariff).IsRequired();

        b.HasOne(e => e.Clearer).WithMany()
            .HasForeignKey(e => e.ClearerId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(e => e.TradePlatform).WithMany()
            .HasForeignKey(e => e.TradePlatformId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Property(e => e.Comment).HasMaxLength(500);
        b.Property(e => e.ExternalId).HasMaxLength(64);

        b.Property(e => e.RowVersion).IsRowVersion();
        b.Property(e => e.CreatedBy).HasMaxLength(100);
        b.Property(e => e.UpdatedBy).HasMaxLength(100);

        b.HasIndex(e => new { e.Status, e.AccountType });

        b.HasMany(e => e.Holders)
            .WithOne(h => h.Account)
            .HasForeignKey(h => h.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ClearerConfiguration : IEntityTypeConfiguration<Clearer>
{
    public void Configure(EntityTypeBuilder<Clearer> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(e => e.Name).IsUnique();
        b.Property(e => e.Description).HasMaxLength(500);
        b.Property(e => e.IsActive).HasDefaultValue(true);
    }
}

public sealed class TradePlatformConfiguration : IEntityTypeConfiguration<TradePlatform>
{
    public void Configure(EntityTypeBuilder<TradePlatform> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(e => e.Name).IsUnique();
        b.Property(e => e.Description).HasMaxLength(500);
        b.Property(e => e.IsActive).HasDefaultValue(true);
    }
}

public sealed class AccountHolderConfiguration : IEntityTypeConfiguration<AccountHolder>
{
    public void Configure(EntityTypeBuilder<AccountHolder> b)
    {
        b.HasKey(e => new { e.AccountId, e.ClientId, e.Role });

        b.HasOne(e => e.Account)
            .WithMany(a => a.Holders)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(e => e.Client)
            .WithMany(c => c.AccountHolders)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Property(e => e.IsPrimary).HasDefaultValue(false);
        b.Property(e => e.AddedAt).IsRequired();
    }
}
