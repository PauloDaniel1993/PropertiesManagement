using Alsappan.Application.Pets;
using Alsappan.Domain.Pets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Pets;

public sealed class PetDocumentLinkConfiguration : IEntityTypeConfiguration<PetDocumentLink>
{
  public void Configure(EntityTypeBuilder<PetDocumentLink> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("pet_document_links");
    builder.HasKey(link => link.Id);
    builder.Property(link => link.OrganizationId)
      .IsRequired();
    builder.Property(link => link.PetId)
      .IsRequired();
    builder.Property(link => link.DocumentId)
      .IsRequired();
    builder.Property(link => link.Kind)
      .HasConversion(
        kind => PetCatalog.GetDocumentKindCode(kind),
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

    builder.HasIndex(link => new { link.OrganizationId, link.PetId });
    builder.HasIndex(link => new { link.OrganizationId, link.DocumentId });
    builder.HasIndex(link => new { link.OrganizationId, link.PetId, link.DocumentId, link.Kind })
      .IsUnique();
    builder.HasIndex(link => new { link.OrganizationId, link.DeletedAt });
  }

  private static PetDocumentKind ParseDocumentKind(string code) =>
    PetCatalog.TryParseDocumentKind(code, out var kind)
      ? kind
      : PetDocumentKind.VaccinationRecord;
}
