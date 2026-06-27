using Alsappan.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Payments;

public sealed class PaymentReceiptLinkConfiguration : IEntityTypeConfiguration<PaymentReceiptLink>
{
  public void Configure(EntityTypeBuilder<PaymentReceiptLink> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("payment_receipt_links");
    builder.HasKey(link => link.Id);
    builder.Property(link => link.OrganizationId)
      .IsRequired();
    builder.Property(link => link.ChargeId)
      .IsRequired();
    builder.Property(link => link.DocumentId)
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

    builder.HasIndex(link => new { link.OrganizationId, link.ChargeId });
    builder.HasIndex(link => new { link.OrganizationId, link.DocumentId });
    builder.HasIndex(link => new { link.OrganizationId, link.ChargeId, link.DocumentId })
      .IsUnique();
    builder.HasIndex(link => new { link.OrganizationId, link.DeletedAt });
  }
}
