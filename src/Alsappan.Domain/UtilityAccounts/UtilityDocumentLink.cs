using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.UtilityAccounts;

public sealed class UtilityDocumentLink : TenantScopedEntity<EntityId>
{
  private UtilityDocumentLink()
  {
  }

  private UtilityDocumentLink(
    EntityId id,
    OrganizationId organizationId,
    EntityId utilityAccountId,
    EntityId documentId,
    UtilityDocumentKind kind,
    string? label,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    UtilityAccountId = utilityAccountId;
    DocumentId = documentId;
    Kind = kind;
    Label = UtilityAccountCode.Optional(label, 160, nameof(label));
  }

  public EntityId UtilityAccountId { get; private set; }

  public EntityId DocumentId { get; private set; }

  public UtilityDocumentKind Kind { get; private set; }

  public string? Label { get; private set; }

  public static UtilityDocumentLink Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId utilityAccountId,
    EntityId documentId,
    UtilityDocumentKind kind,
    string? label,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(id, organizationId, utilityAccountId, documentId, kind, label, createdAt, createdByUserId);
}
