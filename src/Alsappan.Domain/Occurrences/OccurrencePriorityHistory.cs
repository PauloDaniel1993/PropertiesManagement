using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Occurrences;

public sealed class OccurrencePriorityHistory : TenantScopedEntity<EntityId>
{
  private OccurrencePriorityHistory()
  {
  }

  private OccurrencePriorityHistory(
    EntityId id,
    OrganizationId organizationId,
    EntityId occurrenceId,
    OccurrencePriority? previousPriority,
    OccurrencePriority newPriority,
    string? notes,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    OccurrenceId = occurrenceId;
    PreviousPriority = previousPriority;
    NewPriority = newPriority;
    Notes = OccurrenceCode.Optional(notes, 1000, nameof(notes));
  }

  public EntityId OccurrenceId { get; private set; }

  public OccurrencePriority? PreviousPriority { get; private set; }

  public OccurrencePriority NewPriority { get; private set; }

  public string? Notes { get; private set; }

  public static OccurrencePriorityHistory Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId occurrenceId,
    OccurrencePriority? previousPriority,
    OccurrencePriority newPriority,
    string? notes,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(id, organizationId, occurrenceId, previousPriority, newPriority, notes, createdAt, createdByUserId);
}
