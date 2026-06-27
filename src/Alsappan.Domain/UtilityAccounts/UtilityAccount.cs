using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;
using Alsappan.Domain.Common.ValueObjects;

namespace Alsappan.Domain.UtilityAccounts;

public sealed class UtilityAccount : TenantScopedEntity<EntityId>
{
  private readonly List<UtilityDocumentLink> documentLinks = [];

  private UtilityAccount()
  {
  }

  private UtilityAccount(
    EntityId id,
    OrganizationId organizationId,
    EntityId? propertyId,
    EntityId? contractId,
    EntityId? residentId,
    UtilityAccountType type,
    UtilityResponsibility responsibility,
    string title,
    string? description,
    DateOnly billingPeriodStart,
    DateOnly billingPeriodEnd,
    DateOnly dueDate,
    Money amount,
    string? notes,
    string? propertySearchText,
    string? contractSearchText,
    string? residentSearchText,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    ApplyMetadata(
      propertyId,
      contractId,
      residentId,
      type,
      responsibility,
      title,
      description,
      billingPeriodStart,
      billingPeriodEnd,
      dueDate,
      amount,
      notes,
      propertySearchText,
      contractSearchText,
      residentSearchText);
    Status = UtilityAccountStatus.Open;
    PaidAmount = Money.Zero(amount.Currency);
  }

  public EntityId? PropertyId { get; private set; }

  public EntityId? ContractId { get; private set; }

  public EntityId? ResidentId { get; private set; }

  public UtilityAccountType Type { get; private set; }

  public UtilityResponsibility Responsibility { get; private set; }

  public string Title { get; private set; } = string.Empty;

  public string? Description { get; private set; }

  public DateOnly BillingPeriodStart { get; private set; }

  public DateOnly BillingPeriodEnd { get; private set; }

  public DateOnly DueDate { get; private set; }

  public Money Amount { get; private set; } = null!;

  public Money PaidAmount { get; private set; } = null!;

  public DateOnly? PaidOn { get; private set; }

  public string? PaymentMethod { get; private set; }

  public string? BankReference { get; private set; }

  public UtilityAccountStatus Status { get; private set; }

  public string? Notes { get; private set; }

  public string SearchText { get; private set; } = string.Empty;

  public IReadOnlyCollection<UtilityDocumentLink> DocumentLinks => documentLinks.AsReadOnly();

  public static UtilityAccount Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId? propertyId,
    EntityId? contractId,
    EntityId? residentId,
    UtilityAccountType type,
    UtilityResponsibility responsibility,
    string title,
    string? description,
    DateOnly billingPeriodStart,
    DateOnly billingPeriodEnd,
    DateOnly dueDate,
    Money amount,
    string? notes,
    string? propertySearchText,
    string? contractSearchText,
    string? residentSearchText,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null)
  {
    ArgumentNullException.ThrowIfNull(amount);

    return new UtilityAccount(
      id,
      organizationId,
      propertyId,
      contractId,
      residentId,
      type,
      responsibility,
      title,
      description,
      billingPeriodStart,
      billingPeriodEnd,
      dueDate,
      amount,
      notes,
      propertySearchText,
      contractSearchText,
      residentSearchText,
      createdAt,
      createdByUserId);
  }

  public void Update(
    EntityId? propertyId,
    EntityId? contractId,
    EntityId? residentId,
    UtilityAccountType type,
    UtilityResponsibility responsibility,
    string title,
    string? description,
    DateOnly billingPeriodStart,
    DateOnly billingPeriodEnd,
    DateOnly dueDate,
    Money amount,
    string? notes,
    string? propertySearchText,
    string? contractSearchText,
    string? residentSearchText,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    EnsureCanMutate();
    ArgumentNullException.ThrowIfNull(amount);
    if (Status == UtilityAccountStatus.Paid && amount.Amount < PaidAmount.Amount)
    {
      throw new InvalidOperationException("Utility amount cannot be lower than the paid amount.");
    }

    ApplyMetadata(
      propertyId,
      contractId,
      residentId,
      type,
      responsibility,
      title,
      description,
      billingPeriodStart,
      billingPeriodEnd,
      dueDate,
      amount,
      notes,
      propertySearchText,
      contractSearchText,
      residentSearchText);
    if (Status != UtilityAccountStatus.Paid)
    {
      PaidAmount = Money.Zero(amount.Currency);
    }

    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void LinkDocument(
    EntityId documentId,
    UtilityDocumentKind kind,
    string? label,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
  {
    EnsureCanMutate();
    if (documentLinks.Any(link => link.DocumentId == documentId && link.Kind == kind && !link.IsDeleted))
    {
      return;
    }

    documentLinks.Add(UtilityDocumentLink.Create(
      EntityId.New(),
      OrganizationId,
      Id,
      documentId,
      kind,
      label,
      createdAt,
      createdByUserId));
    MarkUpdated(createdAt, createdByUserId);
  }

  public void MarkPaid(
    Money paidAmount,
    DateOnly paidOn,
    string? paymentMethod,
    string? bankReference,
    EntityId? receiptDocumentId,
    string? notes,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    EnsureCanMutate();
    ArgumentNullException.ThrowIfNull(paidAmount);
    if (!string.Equals(paidAmount.Currency, Amount.Currency, StringComparison.OrdinalIgnoreCase))
    {
      throw new InvalidOperationException("Paid amount currency must match the utility amount currency.");
    }

    if (paidAmount.Amount <= 0 || paidAmount.Amount != Amount.Amount)
    {
      throw new ArgumentOutOfRangeException(nameof(paidAmount), "Paid amount must match the utility amount.");
    }

    PaidAmount = paidAmount;
    PaidOn = paidOn == default ? throw new ArgumentException("Paid date is required.", nameof(paidOn)) : paidOn;
    PaymentMethod = UtilityAccountCode.Optional(paymentMethod, 80, nameof(paymentMethod));
    BankReference = UtilityAccountCode.Optional(bankReference, 160, nameof(bankReference));
    Notes = UtilityAccountCode.Optional(notes, 1000, nameof(notes)) ?? Notes;
    Status = UtilityAccountStatus.Paid;

    if (receiptDocumentId.HasValue)
    {
      LinkDocument(receiptDocumentId.Value, UtilityDocumentKind.Receipt, "Recibo", updatedAt, updatedByUserId);
      return;
    }

    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Cancel(DateTimeOffset updatedAt, UserId? updatedByUserId, string? notes = null)
  {
    if (Status is UtilityAccountStatus.Archived or UtilityAccountStatus.Cancelled)
    {
      return;
    }

    Status = UtilityAccountStatus.Cancelled;
    Notes = UtilityAccountCode.Optional(notes, 1000, nameof(notes)) ?? Notes;
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Archive(DateTimeOffset deletedAt, UserId? deletedByUserId)
  {
    if (IsDeleted)
    {
      return;
    }

    Status = UtilityAccountStatus.Archived;
    MarkDeleted(deletedAt, deletedByUserId);
  }

  public void Restore(DateTimeOffset restoredAt, UserId? restoredByUserId)
  {
    if (!IsDeleted)
    {
      return;
    }

    DeletedAt = null;
    DeletedByUserId = null;
    Status = PaidAmount.Amount >= Amount.Amount ? UtilityAccountStatus.Paid : UtilityAccountStatus.Open;
    MarkUpdated(restoredAt, restoredByUserId);
  }

  public Money Balance() => Amount.Subtract(PaidAmount);

  public UtilityAccountStatus GetEffectiveStatus(DateOnly today)
  {
    if (IsDeleted || Status == UtilityAccountStatus.Archived)
    {
      return UtilityAccountStatus.Archived;
    }

    if (Status is UtilityAccountStatus.Cancelled or UtilityAccountStatus.Disputed or UtilityAccountStatus.Paid)
    {
      return Status;
    }

    return DueDate < today && Balance().Amount > 0
      ? UtilityAccountStatus.Overdue
      : UtilityAccountStatus.Open;
  }

  private void ApplyMetadata(
    EntityId? propertyId,
    EntityId? contractId,
    EntityId? residentId,
    UtilityAccountType type,
    UtilityResponsibility responsibility,
    string title,
    string? description,
    DateOnly billingPeriodStart,
    DateOnly billingPeriodEnd,
    DateOnly dueDate,
    Money amount,
    string? notes,
    string? propertySearchText,
    string? contractSearchText,
    string? residentSearchText)
  {
    ArgumentNullException.ThrowIfNull(amount);
    if (amount.Amount <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(amount), "Utility amount must be positive.");
    }

    if (billingPeriodStart == default || billingPeriodEnd == default || billingPeriodEnd < billingPeriodStart)
    {
      throw new ArgumentException("Billing period is invalid.", nameof(billingPeriodEnd));
    }

    PropertyId = propertyId;
    ContractId = contractId;
    ResidentId = residentId;
    Type = type;
    Responsibility = responsibility;
    Title = UtilityAccountCode.Required(title, 200, nameof(title));
    Description = UtilityAccountCode.Optional(description, 1000, nameof(description));
    BillingPeriodStart = billingPeriodStart;
    BillingPeriodEnd = billingPeriodEnd;
    DueDate = dueDate == default ? throw new ArgumentException("Due date is required.", nameof(dueDate)) : dueDate;
    Amount = amount;
    Notes = UtilityAccountCode.Optional(notes, 1000, nameof(notes));
    SearchText = UtilityAccountCode.NormalizeSearchText(
      Id.Value.ToString("D"),
      PropertyId?.Value.ToString("D"),
      ContractId?.Value.ToString("D"),
      ResidentId?.Value.ToString("D"),
      Title,
      Description,
      Type.ToString(),
      Responsibility.ToString(),
      propertySearchText,
      contractSearchText,
      residentSearchText);
  }

  private void EnsureCanMutate()
  {
    if (Status is UtilityAccountStatus.Archived or UtilityAccountStatus.Cancelled)
    {
      throw new InvalidOperationException("Cancelled or archived utility accounts cannot be changed.");
    }
  }
}
