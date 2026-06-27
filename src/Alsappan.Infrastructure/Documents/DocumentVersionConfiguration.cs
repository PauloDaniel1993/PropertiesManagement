using Alsappan.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Documents;

public sealed class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
  public void Configure(EntityTypeBuilder<DocumentVersion> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("document_versions");
    builder.HasKey(version => version.Id);
    builder.Property(version => version.OrganizationId)
      .IsRequired();
    builder.Property(version => version.DocumentId)
      .IsRequired();
    builder.Property(version => version.VersionNumber)
      .IsRequired();
    builder.Property(version => version.FileName)
      .HasMaxLength(180)
      .IsRequired();
    builder.Property(version => version.ContentType)
      .HasMaxLength(140)
      .IsRequired();
    builder.Property(version => version.SizeBytes)
      .IsRequired();
    builder.Property(version => version.StorageKey)
      .HasMaxLength(600)
      .IsRequired();
    builder.Property(version => version.Notes)
      .HasMaxLength(500);
    builder.Property(version => version.CreatedAt)
      .IsRequired();
    builder.Property(version => version.CreatedByUserId);
    builder.Property(version => version.UpdatedAt);
    builder.Property(version => version.UpdatedByUserId);
    builder.Property(version => version.DeletedAt);
    builder.Property(version => version.DeletedByUserId);
    builder.Property(version => version.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();
    builder.HasIndex(version => new { version.OrganizationId, version.DocumentId, version.VersionNumber })
      .IsUnique();
    builder.HasIndex(version => new { version.OrganizationId, version.StorageKey });
    builder.HasIndex(version => new { version.OrganizationId, version.DeletedAt });
  }
}
