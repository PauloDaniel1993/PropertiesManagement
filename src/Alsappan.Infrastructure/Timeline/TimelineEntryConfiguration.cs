using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Timeline;

public sealed class TimelineEntryConfiguration : IEntityTypeConfiguration<TimelineEntry>
{
  public void Configure(EntityTypeBuilder<TimelineEntry> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("TimelineEntries");
    builder.HasKey(entry => entry.Id);

    builder.Property(entry => entry.ActorUserId)
      .HasConversion<Guid?>(
        id => id.HasValue ? id.Value.Value : null,
        value => value.HasValue ? new(value.Value) : null);

    builder.Property(entry => entry.ModuleName).HasMaxLength(96).IsRequired();
    builder.Property(entry => entry.EventName).HasMaxLength(160).IsRequired();
    builder.Property(entry => entry.ActorKind).HasMaxLength(48).IsRequired();
    builder.Property(entry => entry.ActorDisplayName).HasMaxLength(160);
    builder.Property(entry => entry.SubjectEntityType).HasMaxLength(96).IsRequired();
    builder.Property(entry => entry.SubjectEntityId).HasMaxLength(96).IsRequired();
    builder.Property(entry => entry.SubjectDisplayName).HasMaxLength(240);
    builder.Property(entry => entry.RelatedEntitiesJson).HasColumnType("jsonb").IsRequired();
    builder.Property(entry => entry.DataJson).HasColumnType("jsonb").IsRequired();
    builder.Property(entry => entry.CorrelationId).HasMaxLength(128);

    builder.HasIndex(entry => new { entry.OrganizationId, entry.OccurredAt });
    builder.HasIndex(entry => new { entry.OrganizationId, entry.SubjectEntityType, entry.SubjectEntityId });
    builder.HasIndex(entry => new { entry.OrganizationId, entry.EventName, entry.OccurredAt });
    builder.HasIndex(entry => new { entry.OrganizationId, entry.ActorUserId, entry.OccurredAt });
  }
}
