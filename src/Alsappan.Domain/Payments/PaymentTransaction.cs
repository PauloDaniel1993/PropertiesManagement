using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;
using Alsappan.Domain.Common.ValueObjects;

namespace Alsappan.Domain.Payments;

public sealed class PaymentTransaction : TenantScopedEntity<EntityId>
{
  private PaymentTransaction()
  {
  }

  private PaymentTransaction(
    EntityId id,
    OrganizationId organizationId,
    EntityId chargeId,
    Money amount,
    PaymentMethod method,
    DateOnly settledOn,
    string? bankReference,
    string? providerCode,
    string? providerReference,
    EntityId? receiptDocumentId,
    string? notes,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    ArgumentNullException.ThrowIfNull(amount);

    if (amount.Amount <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(amount), "Transaction amount must be positive.");
    }

    ChargeId = chargeId;
    Amount = amount;
    Method = method;
    SettledOn = settledOn;
    BankReference = PaymentCode.Optional(bankReference, 160, nameof(bankReference));
    ProviderCode = PaymentCode.Optional(providerCode, 80, nameof(providerCode));
    ProviderReference = PaymentCode.Optional(providerReference, 160, nameof(providerReference));
    ReceiptDocumentId = receiptDocumentId;
    Notes = PaymentCode.Optional(notes, 1000, nameof(notes));
    Status = PaymentTransactionStatus.Active;
  }

  public EntityId ChargeId { get; private set; }

  public Money Amount { get; private set; } = null!;

  public PaymentMethod Method { get; private set; }

  public DateOnly SettledOn { get; private set; }

  public string? BankReference { get; private set; }

  public string? ProviderCode { get; private set; }

  public string? ProviderReference { get; private set; }

  public EntityId? ReceiptDocumentId { get; private set; }

  public string? Notes { get; private set; }

  public PaymentTransactionStatus Status { get; private set; }

  public bool IsReversed => Status == PaymentTransactionStatus.Reversed;

  public static PaymentTransaction Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId chargeId,
    Money amount,
    PaymentMethod method,
    DateOnly settledOn,
    string? bankReference,
    string? providerCode,
    string? providerReference,
    EntityId? receiptDocumentId,
    string? notes,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null)
  {
    ArgumentNullException.ThrowIfNull(amount);

    return new(
      id,
      organizationId,
      chargeId,
      amount,
      method,
      settledOn,
      bankReference,
      providerCode,
      providerReference,
      receiptDocumentId,
      notes,
      createdAt,
      createdByUserId);
  }

  public void Reverse(DateTimeOffset reversedAt, UserId? reversedByUserId, string? notes = null)
  {
    if (Status == PaymentTransactionStatus.Reversed)
    {
      return;
    }

    Status = PaymentTransactionStatus.Reversed;
    Notes = PaymentCode.Optional(notes, 1000, nameof(notes)) ?? Notes;
    MarkUpdated(reversedAt, reversedByUserId);
  }
}
