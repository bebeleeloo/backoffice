using Broker.Backoffice.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Broker.Backoffice.Infrastructure.Persistence.Configurations;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> b)
    {
        b.HasKey(e => e.Id);
        b.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
        b.HasOne(e => e.User).WithMany(u => u.UserRoles).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(e => e.Role).WithMany(r => r.UserRoles).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Cascade);
        b.Property(e => e.CreatedBy).HasMaxLength(100);
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.HasKey(e => e.Id);
        b.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
        b.HasOne(e => e.Role).WithMany(r => r.RolePermissions).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(e => e.Permission).WithMany(p => p.RolePermissions).HasForeignKey(e => e.PermissionId).OnDelete(DeleteBehavior.Cascade);
        b.Property(e => e.CreatedBy).HasMaxLength(100);
    }
}

public sealed class UserPermissionOverrideConfiguration : IEntityTypeConfiguration<UserPermissionOverride>
{
    public void Configure(EntityTypeBuilder<UserPermissionOverride> b)
    {
        b.HasKey(e => e.Id);
        b.HasIndex(e => new { e.UserId, e.PermissionId }).IsUnique();
        b.HasOne(e => e.User).WithMany(u => u.PermissionOverrides).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(e => e.Permission).WithMany().HasForeignKey(e => e.PermissionId).OnDelete(DeleteBehavior.Cascade);
        b.Property(e => e.CreatedBy).HasMaxLength(100);
    }
}

public sealed class DataScopeConfiguration : IEntityTypeConfiguration<DataScope>
{
    public void Configure(EntityTypeBuilder<DataScope> b)
    {
        b.HasKey(e => e.Id);
        b.HasIndex(e => new { e.UserId, e.ScopeType, e.ScopeValue }).IsUnique();
        b.HasOne(e => e.User).WithMany(u => u.DataScopes).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Property(e => e.ScopeType).HasMaxLength(100).IsRequired();
        b.Property(e => e.ScopeValue).HasMaxLength(500).IsRequired();
        b.Property(e => e.CreatedBy).HasMaxLength(100);
    }
}

public sealed class UserRefreshTokenConfiguration : IEntityTypeConfiguration<UserRefreshToken>
{
    public void Configure(EntityTypeBuilder<UserRefreshToken> b)
    {
        b.HasKey(e => e.Id);
        b.HasIndex(e => e.TokenHash).IsUnique();
        b.HasOne(e => e.User).WithMany(u => u.RefreshTokens).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Property(e => e.TokenHash).HasMaxLength(128).IsRequired();
        b.Property(e => e.ReplacedByTokenHash).HasMaxLength(128);
        b.Ignore(e => e.IsExpired);
        b.Ignore(e => e.IsRevoked);
        b.Ignore(e => e.IsActive);
    }
}
