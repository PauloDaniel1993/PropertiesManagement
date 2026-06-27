using Alsappan.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Settings;

public sealed class DomainCatalogSettingConfiguration : IEntityTypeConfiguration<DomainCatalogSetting>
{
  public void Configure(EntityTypeBuilder<DomainCatalogSetting> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("domain_catalog_settings");
    builder.HasKey(item => item.Id);

    builder.Property(item => item.OrganizationId)
      .IsRequired();
    builder.Property(item => item.CatalogType)
      .HasMaxLength(80)
      .IsRequired();
    builder.Property(item => item.Code)
      .HasMaxLength(96)
      .IsRequired();
    builder.Property(item => item.LabelPtBr)
      .HasMaxLength(120)
      .IsRequired();
    builder.Property(item => item.LabelEnUs)
      .HasMaxLength(120)
      .IsRequired();
    builder.Property(item => item.CreatedAt)
      .IsRequired();
    builder.Property(item => item.CreatedByUserId);
    builder.Property(item => item.UpdatedAt);
    builder.Property(item => item.UpdatedByUserId);
    builder.Property(item => item.DeletedAt);
    builder.Property(item => item.DeletedByUserId);
    builder.Property(item => item.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.HasIndex(item => new { item.OrganizationId, item.CatalogType });
    builder.HasIndex(item => new { item.OrganizationId, item.CatalogType, item.Code })
      .IsUnique();
    builder.HasIndex(item => new { item.OrganizationId, item.CatalogType, item.SortOrder });
    builder.HasIndex(item => new { item.OrganizationId, item.DeletedAt });
  }
}
