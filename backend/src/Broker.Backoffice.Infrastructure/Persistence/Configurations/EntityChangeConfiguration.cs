using Broker.Backoffice.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Broker.Backoffice.Infrastructure.Persistence.Configurations;

public sealed class EntityChangeConfiguration : IEntityTypeConfiguration<EntityChange>
{
    public void Configure(EntityTypeBuilder<EntityChange> b)
    {
        b.HasKey(e => e.Id);

        b.Property(e => e.OperationId).IsRequired();
        b.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
        b.Property(e => e.EntityId).HasMaxLength(100).IsRequired();
        b.Property(e => e.EntityDisplayName).HasMaxLength(200);
        b.Property(e => e.RelatedEntityType).HasMaxLength(100);
        b.Property(e => e.RelatedEntityId).HasMaxLength(200);
        b.Property(e => e.RelatedEntityDisplayName).HasMaxLength(200);
        b.Property(e => e.ChangeType).HasMaxLength(20).IsRequired();
        b.Property(e => e.FieldName).HasMaxLength(200).IsRequired();
        b.Property(e => e.OldValue).HasMaxLength(1000);
        b.Property(e => e.NewValue).HasMaxLength(1000);
        b.Property(e => e.UserId).HasMaxLength(100);
        b.Property(e => e.UserName).HasMaxLength(100);
        b.Property(e => e.Timestamp).IsRequired();

        b.HasIndex(e => new { e.EntityType, e.EntityId });
        b.HasIndex(e => e.OperationId);
        b.HasIndex(e => e.Timestamp);
    }
}
