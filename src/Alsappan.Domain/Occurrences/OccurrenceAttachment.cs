using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Occurrences;

public sealed class OccurrenceAttachment : TenantScopedEntity<EntityId>
{
  private OccurrenceAttachment()
  {
  }

  private OccurrenceAttachment(
    EntityId id,
    OrganizationId organizationId,
    EntityId occurrenceId,
    EntityId documentId,
    string? label,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    OccurrenceId = occurrenceId;
    DocumentId = documentId;
    Label = OccurrenceCode.Optional(label, 160, nameof(label));
  }

  public EntityId OccurrenceId { get; private set; }

  public EntityId DocumentId { get; private set; }

  public string? Label { get; private set; }

  public static OccurrenceAttachment Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId occurrenceId,
    EntityId documentId,
    string? label,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(id, organizationId, occurrenceId, documentId, label, createdAt, createdByUserId);
}
