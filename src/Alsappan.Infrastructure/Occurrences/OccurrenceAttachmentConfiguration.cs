using Alsappan.Domain.Occurrences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Occurrences;

public sealed class OccurrenceAttachmentConfiguration : IEntityTypeConfiguration<OccurrenceAttachment>
{
  public void Configure(EntityTypeBuilder<OccurrenceAttachment> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("occurrence_attachments");
    builder.HasKey(attachment => attachment.Id);
    builder.Property(attachment => attachment.OrganizationId)
      .IsRequired();
    builder.Property(attachment => attachment.OccurrenceId)
      .IsRequired();
    builder.Property(attachment => attachment.DocumentId)
      .IsRequired();
    builder.Property(attachment => attachment.Label)
      .HasMaxLength(160);
    builder.Property(attachment => attachment.CreatedAt)
      .IsRequired();
    builder.Property(attachment => attachment.CreatedByUserId);
    builder.Property(attachment => attachment.UpdatedAt);
    builder.Property(attachment => attachment.UpdatedByUserId);
    builder.Property(attachment => attachment.DeletedAt);
    builder.Property(attachment => attachment.DeletedByUserId);
    builder.Property(attachment => attachment.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.HasIndex(attachment => new { attachment.OrganizationId, attachment.OccurrenceId });
    builder.HasIndex(attachment => new { attachment.OrganizationId, attachment.DocumentId });
    builder.HasIndex(attachment => new { attachment.OrganizationId, attachment.OccurrenceId, attachment.DocumentId })
      .IsUnique();
    builder.HasIndex(attachment => new { attachment.OrganizationId, attachment.DeletedAt });
  }
}
