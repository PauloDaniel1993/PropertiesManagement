using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Payments;

public sealed class PaymentReceiptLink : TenantScopedEntity<EntityId>
{
  private PaymentReceiptLink()
  {
  }

  private PaymentReceiptLink(
    EntityId id,
    OrganizationId organizationId,
    EntityId chargeId,
    EntityId documentId,
    string? label,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    ChargeId = chargeId;
    DocumentId = documentId;
    Label = PaymentCode.Optional(label, 160, nameof(label));
  }

  public EntityId ChargeId { get; private set; }

  public EntityId DocumentId { get; private set; }

  public string? Label { get; private set; }

  public static PaymentReceiptLink Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId chargeId,
    EntityId documentId,
    string? label,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(id, organizationId, chargeId, documentId, label, createdAt, createdByUserId);
}
