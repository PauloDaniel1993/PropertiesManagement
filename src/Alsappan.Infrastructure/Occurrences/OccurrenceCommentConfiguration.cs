using Alsappan.Domain.Occurrences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Occurrences;

public sealed class OccurrenceCommentConfiguration : IEntityTypeConfiguration<OccurrenceComment>
{
  public void Configure(EntityTypeBuilder<OccurrenceComment> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("occurrence_comments");
    builder.HasKey(comment => comment.Id);
    builder.Property(comment => comment.OrganizationId)
      .IsRequired();
    builder.Property(comment => comment.OccurrenceId)
      .IsRequired();
    builder.Property(comment => comment.Body)
      .HasMaxLength(4000)
      .IsRequired();
    builder.Property(comment => comment.IsInternal)
      .IsRequired();
    builder.Property(comment => comment.CreatedAt)
      .IsRequired();
    builder.Property(comment => comment.CreatedByUserId);
    builder.Property(comment => comment.UpdatedAt);
    builder.Property(comment => comment.UpdatedByUserId);
    builder.Property(comment => comment.DeletedAt);
    builder.Property(comment => comment.DeletedByUserId);
    builder.Property(comment => comment.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.HasIndex(comment => new { comment.OrganizationId, comment.OccurrenceId, comment.CreatedAt });
    builder.HasIndex(comment => new { comment.OrganizationId, comment.CreatedByUserId });
    builder.HasIndex(comment => new { comment.OrganizationId, comment.DeletedAt });
  }
}
