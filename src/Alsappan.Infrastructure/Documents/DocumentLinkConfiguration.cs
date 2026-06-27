using Alsappan.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Documents;

public sealed class DocumentLinkConfiguration : IEntityTypeConfiguration<DocumentLink>
{
  public void Configure(EntityTypeBuilder<DocumentLink> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("document_links");
    builder.HasKey(link => link.Id);
    builder.Property(link => link.OrganizationId)
      .IsRequired();
    builder.Property(link => link.DocumentId)
      .IsRequired();
    builder.Property(link => link.EntityType)
      .HasMaxLength(80)
      .IsRequired();
    builder.Property(link => link.EntityId)
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
    builder.HasIndex(link => new { link.OrganizationId, link.EntityType, link.EntityId });
    builder.HasIndex(link => new { link.OrganizationId, link.DocumentId });
    builder.HasIndex(link => new { link.OrganizationId, link.DocumentId, link.EntityType, link.EntityId })
      .IsUnique();
    builder.HasIndex(link => new { link.OrganizationId, link.DeletedAt });
  }
}
