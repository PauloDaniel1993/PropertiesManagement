using Alsappan.Application.Documents;
using Alsappan.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Documents;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<DocumentRecord>
{
  public void Configure(EntityTypeBuilder<DocumentRecord> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("documents");
    builder.HasKey(document => document.Id);

    builder.Property(document => document.OrganizationId)
      .IsRequired();
    builder.Property(document => document.Category)
      .HasConversion(
        category => DocumentCatalog.ToCategoryCode(category),
        code => ParseCategory(code))
      .HasMaxLength(80)
      .IsRequired();
    builder.Property(document => document.Status)
      .HasConversion(
        status => DocumentCatalog.ToStatusCode(status),
        code => ParseStatus(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(document => document.Title)
      .HasMaxLength(180)
      .IsRequired();
    builder.Property(document => document.Description)
      .HasMaxLength(1000);
    builder.Property(document => document.CurrentFileName)
      .HasMaxLength(180)
      .IsRequired();
    builder.Property(document => document.CurrentContentType)
      .HasMaxLength(140)
      .IsRequired();
    builder.Property(document => document.CurrentSizeBytes)
      .IsRequired();
    builder.Property(document => document.CurrentStorageKey)
      .HasMaxLength(600)
      .IsRequired();
    builder.Property(document => document.CurrentVersionNumber)
      .IsRequired();
    builder.Property(document => document.CurrentUploadedAt)
      .IsRequired();
    builder.Property(document => document.CurrentUploadedByUserId);
    builder.Property(document => document.SearchText)
      .HasMaxLength(DocumentCode.MaxSearchTextLength)
      .IsRequired();
    builder.Property(document => document.CreatedAt)
      .IsRequired();
    builder.Property(document => document.CreatedByUserId);
    builder.Property(document => document.UpdatedAt);
    builder.Property(document => document.UpdatedByUserId);
    builder.Property(document => document.DeletedAt);
    builder.Property(document => document.DeletedByUserId);
    builder.Property(document => document.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.HasMany(document => document.Links)
      .WithOne()
      .HasForeignKey(link => link.DocumentId)
      .OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(document => document.Links)
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.HasMany(document => document.Versions)
      .WithOne()
      .HasForeignKey(version => version.DocumentId)
      .OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(document => document.Versions)
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.HasIndex(document => new { document.OrganizationId, document.Category });
    builder.HasIndex(document => new { document.OrganizationId, document.Status });
    builder.HasIndex(document => new { document.OrganizationId, document.CurrentUploadedAt });
    builder.HasIndex(document => new { document.OrganizationId, document.SearchText });
  }

  private static DocumentCategory ParseCategory(string code) =>
    DocumentCatalog.TryParseCategory(code, out var category)
      ? category
      : DocumentCategory.General;

  private static DocumentStatus ParseStatus(string code) =>
    DocumentCatalog.TryParseStatus(code, out var status)
      ? status
      : DocumentStatus.Active;
}
