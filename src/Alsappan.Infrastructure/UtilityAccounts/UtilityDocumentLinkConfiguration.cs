using Alsappan.Domain.UtilityAccounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.UtilityAccounts;

public sealed class UtilityDocumentLinkConfiguration : IEntityTypeConfiguration<UtilityDocumentLink>
{
  public void Configure(EntityTypeBuilder<UtilityDocumentLink> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("utility_document_links");
    builder.HasKey(link => link.Id);
    builder.Property(link => link.OrganizationId)
      .IsRequired();
    builder.Property(link => link.UtilityAccountId)
      .IsRequired();
    builder.Property(link => link.DocumentId)
      .IsRequired();
    builder.Property(link => link.Kind)
      .HasConversion(
        kind => kind == UtilityDocumentKind.Receipt ? "receipt" : "bill",
        code => string.Equals(code, "receipt", StringComparison.OrdinalIgnoreCase)
          ? UtilityDocumentKind.Receipt
          : UtilityDocumentKind.Bill)
      .HasMaxLength(20)
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

    builder.HasIndex(link => new { link.OrganizationId, link.UtilityAccountId });
    builder.HasIndex(link => new { link.OrganizationId, link.DocumentId });
    builder.HasIndex(link => new { link.OrganizationId, link.UtilityAccountId, link.DocumentId, link.Kind })
      .IsUnique();
    builder.HasIndex(link => new { link.OrganizationId, link.DeletedAt });
  }
}
