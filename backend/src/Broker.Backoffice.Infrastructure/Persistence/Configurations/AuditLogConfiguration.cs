using Broker.Backoffice.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Broker.Backoffice.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.HasKey(e => e.Id);
        b.HasIndex(e => e.CreatedAt);
        b.HasIndex(e => e.UserId);
        b.HasIndex(e => e.Action);
        b.HasIndex(e => e.EntityType);
        b.Property(e => e.UserName).HasMaxLength(100);
        b.Property(e => e.Action).HasMaxLength(200).IsRequired();
        b.Property(e => e.EntityType).HasMaxLength(100);
        b.Property(e => e.EntityId).HasMaxLength(100);
        b.Property(e => e.BeforeJson).HasMaxLength(16384);
        b.Property(e => e.AfterJson).HasMaxLength(16384);
        b.Property(e => e.CorrelationId).HasMaxLength(64);
        b.Property(e => e.IpAddress).HasMaxLength(45);
        b.Property(e => e.UserAgent).HasMaxLength(500);
        b.Property(e => e.Path).HasMaxLength(500).IsRequired();
        b.Property(e => e.Method).HasMaxLength(10).IsRequired();
    }
}
