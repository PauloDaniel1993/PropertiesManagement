using Alsappan.Application.Inspections;
using Alsappan.Domain.Inspections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Inspections;

public sealed class InspectionChecklistItemConfiguration : IEntityTypeConfiguration<InspectionChecklistItem>
{
  public void Configure(EntityTypeBuilder<InspectionChecklistItem> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("inspection_checklist_items");
    builder.HasKey(item => item.Id);
    builder.Property(item => item.OrganizationId)
      .IsRequired();
    builder.Property(item => item.InspectionId)
      .IsRequired();
    builder.Property(item => item.AreaName)
      .HasMaxLength(160)
      .IsRequired();
    builder.Property(item => item.ItemName)
      .HasMaxLength(200)
      .IsRequired();
    builder.Property(item => item.IsRequired)
      .IsRequired();
    builder.Property(item => item.ConditionRating)
      .HasConversion(
        rating => InspectionCatalog.ToConditionRatingLabel(rating).Code,
        code => ParseConditionRating(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(item => item.Observations)
      .HasMaxLength(2000);
    builder.Property(item => item.SortOrder)
      .IsRequired();
    builder.Property(item => item.CreatedAt)
      .IsRequired();
    builder.Property(item => item.CreatedByUserId);
    builder.Property(item => item.UpdatedAt);
    builder.Property(item => item.UpdatedByUserId);
    builder.Property(item => item.DeletedAt);
    builder.Property(item => item.DeletedByUserId);
    builder.Property(item => item.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.Ignore(item => item.IsComplete);

    builder.HasIndex(item => new { item.OrganizationId, item.InspectionId });
    builder.HasIndex(item => new { item.OrganizationId, item.InspectionId, item.AreaName });
    builder.HasIndex(item => new { item.OrganizationId, item.ConditionRating });
    builder.HasIndex(item => new { item.OrganizationId, item.DeletedAt });
  }

  private static InspectionConditionRating ParseConditionRating(string code) =>
    InspectionCatalog.TryParseConditionRating(code, out var rating)
      ? rating
      : InspectionConditionRating.Pending;
}
