using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Documents;

public sealed class DocumentLink : TenantScopedEntity<EntityId>
{
  private DocumentLink()
  {
  }

  private DocumentLink(
    EntityId id,
    OrganizationId organizationId,
    EntityId documentId,
    string entityType,
    EntityId entityId,
    string? label,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    DocumentId = documentId;
    EntityType = DocumentCode.Required(DocumentCode.NormalizeCode(entityType), nameof(entityType), 80);
    EntityId = entityId;
    Label = DocumentCode.Optional(label, 160, nameof(label));
  }

  public EntityId DocumentId { get; private set; }

  public string EntityType { get; private set; } = string.Empty;

  public EntityId EntityId { get; private set; }

  public string? Label { get; private set; }

  public static DocumentLink Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId documentId,
    string entityType,
    EntityId entityId,
    string? label,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(id, organizationId, documentId, entityType, entityId, label, createdAt, createdByUserId);
}
