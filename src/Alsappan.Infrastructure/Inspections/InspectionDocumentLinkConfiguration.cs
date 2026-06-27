using Alsappan.Application.Inspections;
using Alsappan.Domain.Inspections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Inspections;

public sealed class InspectionDocumentLinkConfiguration : IEntityTypeConfiguration<InspectionDocumentLink>
{
  public void Configure(EntityTypeBuilder<InspectionDocumentLink> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("inspection_document_links");
    builder.HasKey(link => link.Id);
    builder.Property(link => link.OrganizationId)
      .IsRequired();
    builder.Property(link => link.InspectionId)
      .IsRequired();
    builder.Property(link => link.ChecklistItemId);
    builder.Property(link => link.DocumentId)
      .IsRequired();
    builder.Property(link => link.Kind)
      .HasConversion(
        kind => InspectionCatalog.ToDocumentKindLabel(kind).Code,
        code => ParseDocumentKind(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(link => link.Label)
      .HasMaxLength(160);
    builder.Property(link => link.CreatedAt)
      .IsRequired();
    builder.Property(link => link.CreatedByUserId);
    builder.Property(link => link.UpdatedAt);
    builder.Property(link => link.UpdatedByUserId);
    builder.Property(link => link.DeletedAt);
    builder.Property(link => link.DeletedByUserId);
    builder.Property(link => link.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.HasIndex(link => new { link.OrganizationId, link.InspectionId });
    builder.HasIndex(link => new { link.OrganizationId, link.ChecklistItemId });
    builder.HasIndex(link => new { link.OrganizationId, link.DocumentId });
    builder.HasIndex(link => new { link.OrganizationId, link.InspectionId, link.DocumentId, link.Kind });
    builder.HasIndex(link => new { link.OrganizationId, link.DeletedAt });
  }

  private static InspectionDocumentKind ParseDocumentKind(string code) =>
    InspectionCatalog.TryParseDocumentKind(code, out var kind)
      ? kind
      : InspectionDocumentKind.Attachment;
}
