using Alsappan.Application.Occurrences;
using Alsappan.Domain.Occurrences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Occurrences;

public sealed class OccurrenceStatusHistoryConfiguration : IEntityTypeConfiguration<OccurrenceStatusHistory>
{
  public void Configure(EntityTypeBuilder<OccurrenceStatusHistory> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("occurrence_status_history");
    builder.HasKey(history => history.Id);
    builder.Property(history => history.OrganizationId)
      .IsRequired();
    builder.Property(history => history.OccurrenceId)
      .IsRequired();
    builder.Property(history => history.PreviousStatus)
      .HasConversion(
        status => status.HasValue ? OccurrenceCatalog.ToStatusLabel(status.Value).Code : null,
        code => string.IsNullOrWhiteSpace(code) ? null : ParseStatus(code))
      .HasMaxLength(40);
    builder.Property(history => history.NewStatus)
      .HasConversion(
        status => OccurrenceCatalog.ToStatusLabel(status).Code,
        code => ParseStatus(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(history => history.Notes)
      .HasMaxLength(1000);
    ConfigureMetadata(builder);
    builder.HasIndex(history => new { history.OrganizationId, history.OccurrenceId, history.CreatedAt });
    builder.HasIndex(history => new { history.OrganizationId, history.NewStatus });
  }

  private static OccurrenceStatus ParseStatus(string code) =>
    OccurrenceCatalog.TryParseStatus(code, out var status) ? status : OccurrenceStatus.Open;

  private static void ConfigureMetadata<T>(EntityTypeBuilder<T> builder)
    where T : class
  {
    builder.Property(nameof(OccurrenceStatusHistory.CreatedAt)).IsRequired();
    builder.Property(nameof(OccurrenceStatusHistory.CreatedByUserId));
    builder.Property(nameof(OccurrenceStatusHistory.UpdatedAt));
    builder.Property(nameof(OccurrenceStatusHistory.UpdatedByUserId));
    builder.Property(nameof(OccurrenceStatusHistory.DeletedAt));
    builder.Property(nameof(OccurrenceStatusHistory.DeletedByUserId));
    builder.Property(nameof(OccurrenceStatusHistory.ConcurrencyToken))
      .HasMaxLength(64)
      .IsConcurrencyToken();
  }
}

public sealed class OccurrencePriorityHistoryConfiguration : IEntityTypeConfiguration<OccurrencePriorityHistory>
{
  public void Configure(EntityTypeBuilder<OccurrencePriorityHistory> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("occurrence_priority_history");
    builder.HasKey(history => history.Id);
    builder.Property(history => history.OrganizationId)
      .IsRequired();
    builder.Property(history => history.OccurrenceId)
      .IsRequired();
    builder.Property(history => history.PreviousPriority)
      .HasConversion(
        priority => priority.HasValue ? OccurrenceCatalog.ToPriorityLabel(priority.Value).Code : null,
        code => string.IsNullOrWhiteSpace(code) ? null : ParsePriority(code))
      .HasMaxLength(40);
    builder.Property(history => history.NewPriority)
      .HasConversion(
        priority => OccurrenceCatalog.ToPriorityLabel(priority).Code,
        code => ParsePriority(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(history => history.Notes)
      .HasMaxLength(1000);
    ConfigureMetadata(builder);
    builder.HasIndex(history => new { history.OrganizationId, history.OccurrenceId, history.CreatedAt });
    builder.HasIndex(history => new { history.OrganizationId, history.NewPriority });
  }

  private static OccurrencePriority ParsePriority(string code) =>
    OccurrenceCatalog.TryParsePriority(code, out var priority) ? priority : OccurrencePriority.Medium;

  private static void ConfigureMetadata<T>(EntityTypeBuilder<T> builder)
    where T : class
  {
    builder.Property(nameof(OccurrencePriorityHistory.CreatedAt)).IsRequired();
    builder.Property(nameof(OccurrencePriorityHistory.CreatedByUserId));
    builder.Property(nameof(OccurrencePriorityHistory.UpdatedAt));
    builder.Property(nameof(OccurrencePriorityHistory.UpdatedByUserId));
    builder.Property(nameof(OccurrencePriorityHistory.DeletedAt));
    builder.Property(nameof(OccurrencePriorityHistory.DeletedByUserId));
    builder.Property(nameof(OccurrencePriorityHistory.ConcurrencyToken))
      .HasMaxLength(64)
      .IsConcurrencyToken();
  }
}

public sealed class OccurrenceAssignmentHistoryConfiguration : IEntityTypeConfiguration<OccurrenceAssignmentHistory>
{
  public void Configure(EntityTypeBuilder<OccurrenceAssignmentHistory> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("occurrence_assignment_history");
    builder.HasKey(history => history.Id);
    builder.Property(history => history.OrganizationId)
      .IsRequired();
    builder.Property(history => history.OccurrenceId)
      .IsRequired();
    builder.Property(history => history.PreviousAssignedUserId);
    builder.Property(history => history.NewAssignedUserId);
    builder.Property(history => history.Notes)
      .HasMaxLength(1000);
    builder.Property(history => history.CreatedAt)
      .IsRequired();
    builder.Property(history => history.CreatedByUserId);
    builder.Property(history => history.UpdatedAt);
    builder.Property(history => history.UpdatedByUserId);
    builder.Property(history => history.DeletedAt);
    builder.Property(history => history.DeletedByUserId);
    builder.Property(history => history.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.HasIndex(history => new { history.OrganizationId, history.OccurrenceId, history.CreatedAt });
    builder.HasIndex(history => new { history.OrganizationId, history.NewAssignedUserId });
  }
}
