using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Notifications;

public sealed class NotificationRecordConfiguration : IEntityTypeConfiguration<NotificationRecord>
{
  public void Configure(EntityTypeBuilder<NotificationRecord> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("NotificationRecords");
    builder.HasKey(notification => notification.Id);

    builder.Property(notification => notification.RecipientUserId)
      .HasConversion<Guid?>(
        id => id.HasValue ? id.Value.Value : null,
        value => value.HasValue ? new(value.Value) : null);
    builder.Property(notification => notification.DeletedByUserId)
      .HasConversion<Guid?>(
        id => id.HasValue ? id.Value.Value : null,
        value => value.HasValue ? new(value.Value) : null);

    builder.Property(notification => notification.Category).HasMaxLength(96).IsRequired();
    builder.Property(notification => notification.EventName).HasMaxLength(160).IsRequired();
    builder.Property(notification => notification.Channel).HasMaxLength(48).IsRequired();
    builder.Property(notification => notification.DeliveryStatus).HasMaxLength(48).IsRequired();
    builder.Property(notification => notification.PayloadJson).HasColumnType("jsonb").IsRequired();
    builder.Property(notification => notification.SubjectEntityType).HasMaxLength(96).IsRequired();
    builder.Property(notification => notification.SubjectEntityId).HasMaxLength(96).IsRequired();
    builder.Property(notification => notification.SubjectDisplayName).HasMaxLength(240);
    builder.Property(notification => notification.CorrelationId).HasMaxLength(128);
    builder.Ignore(notification => notification.IsDeleted);

    builder.HasIndex(notification => new { notification.OrganizationId, notification.RecipientUserId, notification.IsRead });
    builder.HasIndex(notification => new { notification.OrganizationId, notification.Category, notification.OccurredAt });
    builder.HasIndex(notification => new { notification.OrganizationId, notification.SubjectEntityType, notification.SubjectEntityId });
  }
}
