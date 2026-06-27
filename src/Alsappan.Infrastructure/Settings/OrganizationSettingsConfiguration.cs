using Alsappan.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Settings;

public sealed class OrganizationSettingsConfiguration : IEntityTypeConfiguration<OrganizationSettings>
{
  public void Configure(EntityTypeBuilder<OrganizationSettings> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("organization_settings");
    builder.HasKey(settings => settings.Id);

    builder.Property(settings => settings.OrganizationId)
      .IsRequired();
    builder.Property(settings => settings.ContactEmail)
      .HasMaxLength(320);
    builder.Property(settings => settings.ContactPhone)
      .HasMaxLength(64);
    builder.Property(settings => settings.ContactWebsite)
      .HasMaxLength(400);
    builder.Property(settings => settings.TimeZone)
      .HasMaxLength(96)
      .IsRequired();
    builder.Property(settings => settings.DefaultLocale)
      .HasMaxLength(16)
      .IsRequired();
    builder.Property(settings => settings.FallbackLocale)
      .HasMaxLength(16)
      .IsRequired();
    builder.Property(settings => settings.EnabledLocales)
      .HasColumnType("text[]")
      .IsRequired();
    builder.Property(settings => settings.EnabledNotificationCategories)
      .HasColumnType("text[]")
      .IsRequired();
    builder.Property(settings => settings.EnabledNotificationChannels)
      .HasColumnType("text[]")
      .IsRequired();
    builder.Property(settings => settings.MfaPolicy)
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(settings => settings.BrandDisplayName)
      .HasMaxLength(120);
    builder.Property(settings => settings.LogoStorageKey)
      .HasMaxLength(600);
    builder.Property(settings => settings.LogoFileName)
      .HasMaxLength(180);
    builder.Property(settings => settings.LogoContentType)
      .HasMaxLength(140);
    builder.Property(settings => settings.LogoAlt)
      .HasMaxLength(140);
    builder.Property(settings => settings.LogoUrl)
      .HasMaxLength(400);
    builder.Property(settings => settings.PrimaryColor)
      .HasMaxLength(16);
    builder.Property(settings => settings.PrimaryForegroundColor)
      .HasMaxLength(16);
    builder.Property(settings => settings.AccentColor)
      .HasMaxLength(16);
    builder.Property(settings => settings.AccentForegroundColor)
      .HasMaxLength(16);
    builder.Property(settings => settings.SupportEmail)
      .HasMaxLength(320);
    builder.Property(settings => settings.SupportPhone)
      .HasMaxLength(64);
    builder.Property(settings => settings.SupportUrl)
      .HasMaxLength(400);
    builder.Property(settings => settings.CreatedAt)
      .IsRequired();
    builder.Property(settings => settings.CreatedByUserId);
    builder.Property(settings => settings.UpdatedAt);
    builder.Property(settings => settings.UpdatedByUserId);
    builder.Property(settings => settings.DeletedAt);
    builder.Property(settings => settings.DeletedByUserId);
    builder.Property(settings => settings.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.HasIndex(settings => settings.OrganizationId)
      .IsUnique();
    builder.HasIndex(settings => new { settings.OrganizationId, settings.DefaultLocale });
    builder.HasIndex(settings => new { settings.OrganizationId, settings.DeletedAt });
  }
}
