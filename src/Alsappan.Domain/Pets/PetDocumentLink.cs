using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Pets;

public sealed class PetDocumentLink : TenantScopedEntity<EntityId>
{
  private PetDocumentLink()
  {
  }

  private PetDocumentLink(
    EntityId id,
    OrganizationId organizationId,
    EntityId petId,
    EntityId documentId,
    PetDocumentKind kind,
    string? label,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    PetId = petId;
    DocumentId = documentId;
    Kind = kind;
    Label = PetCode.Optional(label, 160, nameof(label));
  }

  public EntityId PetId { get; private set; }

  public EntityId DocumentId { get; private set; }

  public PetDocumentKind Kind { get; private set; }

  public string? Label { get; private set; }

  public static PetDocumentLink Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId petId,
    EntityId documentId,
    PetDocumentKind kind,
    string? label,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(id, organizationId, petId, documentId, kind, label, createdAt, createdByUserId);
}
