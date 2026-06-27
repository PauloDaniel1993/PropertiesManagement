using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Notifications;

public sealed class NotificationPreferenceRecordConfiguration :
  IEntityTypeConfiguration<NotificationPreferenceRecord>
{
  public void Configure(EntityTypeBuilder<NotificationPreferenceRecord> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("NotificationPreferenceRecords");
    builder.HasKey(preference => preference.Id);

    builder.Property(preference => preference.UserId)
      .HasConversion(
        id => id.Value,
        value => new(value));

    builder.Property(preference => preference.Category).HasMaxLength(96).IsRequired();
    builder.Property(preference => preference.Channel).HasMaxLength(48).IsRequired();

    builder.HasIndex(preference => new
    {
      preference.OrganizationId,
      preference.UserId,
      preference.Category,
      preference.Channel
    })
      .IsUnique();
    builder.HasIndex(preference => new { preference.OrganizationId, preference.UserId });
  }
}
