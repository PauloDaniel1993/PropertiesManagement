using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Inspections;

public sealed class InspectionDocumentLink : TenantScopedEntity<EntityId>
{
  private InspectionDocumentLink()
  {
  }

  private InspectionDocumentLink(
    EntityId id,
    OrganizationId organizationId,
    EntityId inspectionId,
    EntityId? checklistItemId,
    EntityId documentId,
    InspectionDocumentKind kind,
    string? label,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    InspectionId = inspectionId;
    ChecklistItemId = checklistItemId;
    DocumentId = documentId;
    Kind = kind;
    Label = InspectionCode.Optional(label, 160, nameof(label));
  }

  public EntityId InspectionId { get; private set; }

  public EntityId? ChecklistItemId { get; private set; }

  public EntityId DocumentId { get; private set; }

  public InspectionDocumentKind Kind { get; private set; }

  public string? Label { get; private set; }

  public static InspectionDocumentLink Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId inspectionId,
    EntityId? checklistItemId,
    EntityId documentId,
    InspectionDocumentKind kind,
    string? label,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(
      id,
      organizationId,
      inspectionId,
      checklistItemId,
      documentId,
      kind,
      label,
      createdAt,
      createdByUserId);

  public void Remove(DateTimeOffset deletedAt, UserId? deletedByUserId)
  {
    if (IsDeleted)
    {
      return;
    }

    MarkDeleted(deletedAt, deletedByUserId);
  }
}
