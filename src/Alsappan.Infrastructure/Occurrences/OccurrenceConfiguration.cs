using Alsappan.Application.Occurrences;
using Alsappan.Domain.Occurrences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Occurrences;

public sealed class OccurrenceConfiguration : IEntityTypeConfiguration<Occurrence>
{
  public void Configure(EntityTypeBuilder<Occurrence> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("occurrences");
    builder.HasKey(occurrence => occurrence.Id);

    builder.Property(occurrence => occurrence.OrganizationId)
      .IsRequired();
    builder.Property(occurrence => occurrence.Title)
      .HasMaxLength(200)
      .IsRequired();
    builder.Property(occurrence => occurrence.Description)
      .HasMaxLength(4000)
      .IsRequired();
    builder.Property(occurrence => occurrence.Type)
      .HasConversion(
        type => OccurrenceCatalog.ToTypeLabel(type).Code,
        code => ParseType(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(occurrence => occurrence.Priority)
      .HasConversion(
        priority => OccurrenceCatalog.ToPriorityLabel(priority).Code,
        code => ParsePriority(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(occurrence => occurrence.Status)
      .HasConversion(
        status => OccurrenceCatalog.ToStatusLabel(status).Code,
        code => ParseStatus(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(occurrence => occurrence.PropertyId);
    builder.Property(occurrence => occurrence.ResidentId);
    builder.Property(occurrence => occurrence.ContractId);
    builder.Property(occurrence => occurrence.AssignedUserId);
    builder.Property(occurrence => occurrence.DueDate);
    builder.Property(occurrence => occurrence.ResolvedAt);
    builder.Property(occurrence => occurrence.ResolvedByUserId);
    builder.Property(occurrence => occurrence.ResolutionNotes)
      .HasMaxLength(2000);
    builder.Property(occurrence => occurrence.CancelledAt);
    builder.Property(occurrence => occurrence.CancelledByUserId);
    builder.Property(occurrence => occurrence.CancellationNotes)
      .HasMaxLength(1000);
    builder.Property(occurrence => occurrence.SearchText)
      .HasMaxLength(4000)
      .IsRequired();
    builder.Property(occurrence => occurrence.CreatedAt)
      .IsRequired();
    builder.Property(occurrence => occurrence.CreatedByUserId);
    builder.Property(occurrence => occurrence.UpdatedAt);
    builder.Property(occurrence => occurrence.UpdatedByUserId);
    builder.Property(occurrence => occurrence.DeletedAt);
    builder.Property(occurrence => occurrence.DeletedByUserId);
    builder.Property(occurrence => occurrence.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.Ignore(occurrence => occurrence.IsUnresolved);
    builder.Ignore(occurrence => occurrence.EffectiveStatus);

    builder.HasMany(occurrence => occurrence.Comments)
      .WithOne()
      .HasForeignKey(comment => comment.OccurrenceId)
      .OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(occurrence => occurrence.Comments)
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.HasMany(occurrence => occurrence.Attachments)
      .WithOne()
      .HasForeignKey(attachment => attachment.OccurrenceId)
      .OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(occurrence => occurrence.Attachments)
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.HasMany(occurrence => occurrence.StatusHistory)
      .WithOne()
      .HasForeignKey(history => history.OccurrenceId)
      .OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(occurrence => occurrence.StatusHistory)
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.HasMany(occurrence => occurrence.PriorityHistory)
      .WithOne()
      .HasForeignKey(history => history.OccurrenceId)
      .OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(occurrence => occurrence.PriorityHistory)
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.HasMany(occurrence => occurrence.AssignmentHistory)
      .WithOne()
      .HasForeignKey(history => history.OccurrenceId)
      .OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(occurrence => occurrence.AssignmentHistory)
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.HasIndex(occurrence => new { occurrence.OrganizationId, occurrence.Status });
    builder.HasIndex(occurrence => new { occurrence.OrganizationId, occurrence.Priority });
    builder.HasIndex(occurrence => new { occurrence.OrganizationId, occurrence.Type });
    builder.HasIndex(occurrence => new { occurrence.OrganizationId, occurrence.AssignedUserId });
    builder.HasIndex(occurrence => new { occurrence.OrganizationId, occurrence.PropertyId });
    builder.HasIndex(occurrence => new { occurrence.OrganizationId, occurrence.ResidentId });
    builder.HasIndex(occurrence => new { occurrence.OrganizationId, occurrence.ContractId });
    builder.HasIndex(occurrence => new { occurrence.OrganizationId, occurrence.DueDate });
    builder.HasIndex(occurrence => new { occurrence.OrganizationId, occurrence.CreatedAt });
    builder.HasIndex(occurrence => new { occurrence.OrganizationId, occurrence.SearchText });
    builder.HasIndex(occurrence => new { occurrence.OrganizationId, occurrence.DeletedAt });
  }

  private static OccurrenceType ParseType(string code) =>
    OccurrenceCatalog.TryParseType(code, out var type) ? type : OccurrenceType.Other;

  private static OccurrencePriority ParsePriority(string code) =>
    OccurrenceCatalog.TryParsePriority(code, out var priority) ? priority : OccurrencePriority.Medium;

  private static OccurrenceStatus ParseStatus(string code) =>
    OccurrenceCatalog.TryParseStatus(code, out var status) ? status : OccurrenceStatus.Open;
}
