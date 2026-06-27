using Alsappan.Domain.Inspections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Inspections;

public sealed class InspectionSignatureSlotConfiguration : IEntityTypeConfiguration<InspectionSignatureSlot>
{
  public void Configure(EntityTypeBuilder<InspectionSignatureSlot> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("inspection_signature_slots");
    builder.HasKey(slot => slot.Id);
    builder.Property(slot => slot.OrganizationId)
      .IsRequired();
    builder.Property(slot => slot.InspectionId)
      .IsRequired();
    builder.Property(slot => slot.SignerRole)
      .HasMaxLength(120)
      .IsRequired();
    builder.Property(slot => slot.SignerName)
      .HasMaxLength(160);
    builder.Property(slot => slot.IsRequired)
      .IsRequired();
    builder.Property(slot => slot.SignedAt);
    builder.Property(slot => slot.SignatureDocumentId);
    builder.Property(slot => slot.Notes)
      .HasMaxLength(1000);
    builder.Property(slot => slot.CreatedAt)
      .IsRequired();
    builder.Property(slot => slot.CreatedByUserId);
    builder.Property(slot => slot.UpdatedAt);
    builder.Property(slot => slot.UpdatedByUserId);
    builder.Property(slot => slot.DeletedAt);
    builder.Property(slot => slot.DeletedByUserId);
    builder.Property(slot => slot.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.Ignore(slot => slot.IsSigned);

    builder.HasIndex(slot => new { slot.OrganizationId, slot.InspectionId });
    builder.HasIndex(slot => new { slot.OrganizationId, slot.SignatureDocumentId });
    builder.HasIndex(slot => new { slot.OrganizationId, slot.DeletedAt });
  }
}
