using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Audit;

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
  public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("AuditLogEntries");
    builder.HasKey(entry => entry.Id);

    builder.Property(entry => entry.ActorUserId)
      .HasConversion<Guid?>(
        id => id.HasValue ? id.Value.Value : null,
        value => value.HasValue ? new(value.Value) : null);

    builder.Property(entry => entry.Action).HasMaxLength(160).IsRequired();
    builder.Property(entry => entry.Category).HasMaxLength(32).IsRequired();
    builder.Property(entry => entry.ActorKind).HasMaxLength(48).IsRequired();
    builder.Property(entry => entry.ActorDisplayName).HasMaxLength(160);
    builder.Property(entry => entry.TargetEntityType).HasMaxLength(96).IsRequired();
    builder.Property(entry => entry.TargetEntityId).HasMaxLength(96).IsRequired();
    builder.Property(entry => entry.TargetDisplayName).HasMaxLength(240);
    builder.Property(entry => entry.ChangedFieldsJson).HasColumnType("jsonb").IsRequired();
    builder.Property(entry => entry.ContextJson).HasColumnType("jsonb").IsRequired();
    builder.Property(entry => entry.CorrelationId).HasMaxLength(128);

    builder.HasIndex(entry => new { entry.OrganizationId, entry.OccurredAt });
    builder.HasIndex(entry => new { entry.OrganizationId, entry.TargetEntityType, entry.TargetEntityId });
    builder.HasIndex(entry => new { entry.OrganizationId, entry.ActorUserId, entry.OccurredAt });
    builder.HasIndex(entry => new { entry.OrganizationId, entry.Category, entry.OccurredAt });
  }
}
