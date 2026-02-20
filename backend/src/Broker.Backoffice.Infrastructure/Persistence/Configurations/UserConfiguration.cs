using Broker.Backoffice.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Broker.Backoffice.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Username).HasMaxLength(100).IsRequired();
        b.HasIndex(e => e.Username).IsUnique();
        b.Property(e => e.Email).HasMaxLength(256).IsRequired();
        b.HasIndex(e => e.Email).IsUnique();
        b.Property(e => e.PasswordHash).IsRequired();
        b.Property(e => e.FullName).HasMaxLength(200);
        b.Property(e => e.RowVersion).IsRowVersion();
        b.Property(e => e.CreatedBy).HasMaxLength(100);
        b.Property(e => e.UpdatedBy).HasMaxLength(100);
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(e => e.Name).IsUnique();
        b.Property(e => e.Description).HasMaxLength(500);
        b.Property(e => e.RowVersion).IsRowVersion();
        b.Property(e => e.CreatedBy).HasMaxLength(100);
        b.Property(e => e.UpdatedBy).HasMaxLength(100);
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Code).HasMaxLength(100).IsRequired();
        b.HasIndex(e => e.Code).IsUnique();
        b.Property(e => e.Name).HasMaxLength(200).IsRequired();
        b.Property(e => e.Description).HasMaxLength(500);
        b.Property(e => e.Group).HasMaxLength(100).IsRequired();
        b.Property(e => e.RowVersion).IsRowVersion();
        b.Property(e => e.CreatedBy).HasMaxLength(100);
        b.Property(e => e.UpdatedBy).HasMaxLength(100);
    }
}
