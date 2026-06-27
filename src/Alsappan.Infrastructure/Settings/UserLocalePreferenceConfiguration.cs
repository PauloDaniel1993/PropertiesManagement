using Alsappan.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Settings;

public sealed class UserLocalePreferenceConfiguration : IEntityTypeConfiguration<UserLocalePreference>
{
  public void Configure(EntityTypeBuilder<UserLocalePreference> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("user_locale_preferences");
    builder.HasKey(preference => preference.Id);

    builder.Property(preference => preference.OrganizationId)
      .IsRequired();
    builder.Property(preference => preference.UserId)
      .IsRequired();
    builder.Property(preference => preference.Locale)
      .HasMaxLength(16)
      .IsRequired();
    builder.Property(preference => preference.CreatedAt)
      .IsRequired();
    builder.Property(preference => preference.CreatedByUserId);
    builder.Property(preference => preference.UpdatedAt);
    builder.Property(preference => preference.UpdatedByUserId);
    builder.Property(preference => preference.DeletedAt);
    builder.Property(preference => preference.DeletedByUserId);
    builder.Property(preference => preference.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.HasIndex(preference => new { preference.OrganizationId, preference.UserId })
      .IsUnique();
    builder.HasIndex(preference => new { preference.OrganizationId, preference.Locale });
    builder.HasIndex(preference => new { preference.OrganizationId, preference.DeletedAt });
  }
}
