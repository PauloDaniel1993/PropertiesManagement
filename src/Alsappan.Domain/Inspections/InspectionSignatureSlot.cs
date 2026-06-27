using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Inspections;

public sealed class InspectionSignatureSlot : TenantScopedEntity<EntityId>
{
  private InspectionSignatureSlot()
  {
  }

  private InspectionSignatureSlot(
    EntityId id,
    OrganizationId organizationId,
    EntityId inspectionId,
    string signerRole,
    string? signerName,
    bool isRequired,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    InspectionId = inspectionId;
    SignerRole = InspectionCode.Required(signerRole, 120, nameof(signerRole));
    SignerName = InspectionCode.Optional(signerName, 160, nameof(signerName));
    IsRequired = isRequired;
  }

  public EntityId InspectionId { get; private set; }

  public string SignerRole { get; private set; } = string.Empty;

  public string? SignerName { get; private set; }

  public bool IsRequired { get; private set; }

  public DateTimeOffset? SignedAt { get; private set; }

  public EntityId? SignatureDocumentId { get; private set; }

  public string? Notes { get; private set; }

  public bool IsSigned => SignedAt.HasValue;

  public static InspectionSignatureSlot Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId inspectionId,
    string signerRole,
    string? signerName,
    bool isRequired,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(
      id,
      organizationId,
      inspectionId,
      signerRole,
      signerName,
      isRequired,
      createdAt,
      createdByUserId);

  public void Sign(
    string signerName,
    EntityId? signatureDocumentId,
    string? notes,
    DateTimeOffset signedAt,
    UserId? signedByUserId)
  {
    SignerName = InspectionCode.Required(signerName, 160, nameof(signerName));
    SignatureDocumentId = signatureDocumentId;
    SignedAt = signedAt == default
      ? throw new ArgumentException("Signature timestamp is required.", nameof(signedAt))
      : signedAt;
    Notes = InspectionCode.Optional(notes, 1000, nameof(notes));
    MarkUpdated(signedAt, signedByUserId);
  }

  public void Remove(DateTimeOffset deletedAt, UserId? deletedByUserId)
  {
    if (IsDeleted)
    {
      return;
    }

    MarkDeleted(deletedAt, deletedByUserId);
  }
}
