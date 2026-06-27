using Alsappan.Application.Inspections;
using Alsappan.Domain.Inspections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Inspections;

public sealed class InspectionConfiguration : IEntityTypeConfiguration<Inspection>
{
  public void Configure(EntityTypeBuilder<Inspection> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("inspections");
    builder.HasKey(inspection => inspection.Id);

    builder.Property(inspection => inspection.OrganizationId)
      .IsRequired();
    builder.Property(inspection => inspection.Type)
      .HasConversion(
        type => InspectionCatalog.ToTypeLabel(type).Code,
        code => ParseType(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(inspection => inspection.PropertyId)
      .IsRequired();
    builder.Property(inspection => inspection.ContractId);
    builder.Property(inspection => inspection.ResidentId);
    builder.Property(inspection => inspection.ScheduledAt)
      .IsRequired();
    builder.Property(inspection => inspection.AssignedUserId)
      .IsRequired();
    builder.Property(inspection => inspection.AssignedUserName)
      .HasMaxLength(160);
    builder.Property(inspection => inspection.Status)
      .HasConversion(
        status => InspectionCatalog.ToStatusLabel(status).Code,
        code => ParseStatus(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(inspection => inspection.Title)
      .HasMaxLength(200)
      .IsRequired();
    builder.Property(inspection => inspection.Notes)
      .HasMaxLength(2000);
    builder.Property(inspection => inspection.StartedAt);
    builder.Property(inspection => inspection.StartedByUserId);
    builder.Property(inspection => inspection.CompletedAt);
    builder.Property(inspection => inspection.CompletedByUserId);
    builder.Property(inspection => inspection.CompletionNotes)
      .HasMaxLength(2000);
    builder.Property(inspection => inspection.CancelledAt);
    builder.Property(inspection => inspection.CancelledByUserId);
    builder.Property(inspection => inspection.CancellationReason)
      .HasMaxLength(1000);
    builder.Property(inspection => inspection.SearchText)
      .HasMaxLength(4000)
      .IsRequired();
    builder.Property(inspection => inspection.CreatedAt)
      .IsRequired();
    builder.Property(inspection => inspection.CreatedByUserId);
    builder.Property(inspection => inspection.UpdatedAt);
    builder.Property(inspection => inspection.UpdatedByUserId);
    builder.Property(inspection => inspection.DeletedAt);
    builder.Property(inspection => inspection.DeletedByUserId);
    builder.Property(inspection => inspection.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.Ignore(inspection => inspection.ActiveChecklistItemCount);
    builder.Ignore(inspection => inspection.CompletedChecklistItemCount);
    builder.Ignore(inspection => inspection.CompletionPercentage);

    builder.HasMany(inspection => inspection.ChecklistItems)
      .WithOne()
      .HasForeignKey(item => item.InspectionId)
      .OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(inspection => inspection.ChecklistItems)
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.HasMany(inspection => inspection.DocumentLinks)
      .WithOne()
      .HasForeignKey(link => link.InspectionId)
      .OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(inspection => inspection.DocumentLinks)
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.HasMany(inspection => inspection.SignatureSlots)
      .WithOne()
      .HasForeignKey(slot => slot.InspectionId)
      .OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(inspection => inspection.SignatureSlots)
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.HasIndex(inspection => new { inspection.OrganizationId, inspection.PropertyId });
    builder.HasIndex(inspection => new { inspection.OrganizationId, inspection.ContractId });
    builder.HasIndex(inspection => new { inspection.OrganizationId, inspection.ResidentId });
    builder.HasIndex(inspection => new { inspection.OrganizationId, inspection.AssignedUserId });
    builder.HasIndex(inspection => new { inspection.OrganizationId, inspection.ScheduledAt });
    builder.HasIndex(inspection => new { inspection.OrganizationId, inspection.Status });
    builder.HasIndex(inspection => new { inspection.OrganizationId, inspection.Status, inspection.ScheduledAt });
    builder.HasIndex(inspection => new { inspection.OrganizationId, inspection.SearchText });
    builder.HasIndex(inspection => new { inspection.OrganizationId, inspection.DeletedAt });
  }

  private static InspectionType ParseType(string code) =>
    InspectionCatalog.TryParseType(code, out var type) ? type : InspectionType.Other;

  private static InspectionStatus ParseStatus(string code) =>
    InspectionCatalog.TryParseStatus(code, out var status) ? status : InspectionStatus.Scheduled;
}
