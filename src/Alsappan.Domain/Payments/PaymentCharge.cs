using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;
using Alsappan.Domain.Common.ValueObjects;

namespace Alsappan.Domain.Payments;

public sealed class PaymentCharge : TenantScopedEntity<EntityId>
{
  private readonly List<PaymentTransaction> transactions = [];
  private readonly List<PaymentReceiptLink> receiptLinks = [];

  private PaymentCharge()
  {
  }

  private PaymentCharge(
    EntityId id,
    OrganizationId organizationId,
    EntityId? contractId,
    EntityId? propertyId,
    EntityId? residentId,
    EntityId? utilityAccountId,
    string title,
    string? description,
    DateOnly dueDate,
    Money amount,
    Money? discountAmount,
    Money? penaltyAmount,
    PaymentMethod preferredMethod,
    PaymentReconciliationStatus reconciliationStatus,
    string? notes,
    string? contractSearchText,
    string? propertySearchText,
    string? residentSearchText,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    ApplyMetadata(
      contractId,
      propertyId,
      residentId,
      utilityAccountId,
      title,
      description,
      dueDate,
      amount,
      discountAmount,
      penaltyAmount,
      preferredMethod,
      reconciliationStatus,
      notes,
      contractSearchText,
      propertySearchText,
      residentSearchText);
    Status = PaymentStatus.Pending;
  }

  public EntityId? ContractId { get; private set; }

  public EntityId? PropertyId { get; private set; }

  public EntityId? ResidentId { get; private set; }

  public EntityId? UtilityAccountId { get; private set; }

  public string Title { get; private set; } = string.Empty;

  public string? Description { get; private set; }

  public DateOnly DueDate { get; private set; }

  public Money Amount { get; private set; } = null!;

  public Money DiscountAmount { get; private set; } = null!;

  public Money PenaltyAmount { get; private set; } = null!;

  public PaymentMethod PreferredMethod { get; private set; }

  public PaymentStatus Status { get; private set; }

  public PaymentReconciliationStatus ReconciliationStatus { get; private set; }

  public string? ProviderCode { get; private set; }

  public string? ProviderReference { get; private set; }

  public string? ProviderMetadataJson { get; private set; }

  public string? Notes { get; private set; }

  public string SearchText { get; private set; } = string.Empty;

  public IReadOnlyCollection<PaymentTransaction> Transactions => transactions.AsReadOnly();

  public IReadOnlyCollection<PaymentReceiptLink> ReceiptLinks => receiptLinks.AsReadOnly();

  public static PaymentCharge Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId? contractId,
    EntityId? propertyId,
    EntityId? residentId,
    EntityId? utilityAccountId,
    string title,
    string? description,
    DateOnly dueDate,
    Money amount,
    Money? discountAmount,
    Money? penaltyAmount,
    PaymentMethod preferredMethod,
    PaymentReconciliationStatus reconciliationStatus,
    string? notes,
    string? contractSearchText,
    string? propertySearchText,
    string? residentSearchText,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(
      id,
      organizationId,
      contractId,
      propertyId,
      residentId,
      utilityAccountId,
      title,
      description,
      dueDate,
      amount,
      discountAmount,
      penaltyAmount,
      preferredMethod,
      reconciliationStatus,
      notes,
      contractSearchText,
      propertySearchText,
      residentSearchText,
      createdAt,
      createdByUserId);

  public void Update(
    EntityId? contractId,
    EntityId? propertyId,
    EntityId? residentId,
    EntityId? utilityAccountId,
    string title,
    string? description,
    DateOnly dueDate,
    Money amount,
    Money? discountAmount,
    Money? penaltyAmount,
    PaymentMethod preferredMethod,
    PaymentReconciliationStatus reconciliationStatus,
    string? notes,
    string? contractSearchText,
    string? propertySearchText,
    string? residentSearchText,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    EnsureCanMutate();
    ApplyMetadata(
      contractId,
      propertyId,
      residentId,
      utilityAccountId,
      title,
      description,
      dueDate,
      amount,
      discountAmount,
      penaltyAmount,
      preferredMethod,
      reconciliationStatus,
      notes,
      contractSearchText,
      propertySearchText,
      residentSearchText);
    RecalculateStoredStatus(updatedAt, updatedByUserId);
  }

  public PaymentTransaction RecordTransaction(
    EntityId transactionId,
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
  {
    ArgumentNullException.ThrowIfNull(amount);

    EnsureCanSettle();
    if (!StringComparer.Ordinal.Equals(amount.Currency, Amount.Currency))
    {
      throw new InvalidOperationException("Payment transaction currency must match the charge currency.");
    }

    if (amount.Amount > Balance().Amount)
    {
      throw new ArgumentOutOfRangeException(nameof(amount), "Transaction amount cannot exceed the open balance.");
    }

    var transaction = PaymentTransaction.Create(
      transactionId,
      OrganizationId,
      Id,
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
    transactions.Add(transaction);
    if (!string.IsNullOrWhiteSpace(providerCode) && !string.IsNullOrWhiteSpace(providerReference))
    {
      ReconciliationStatus = PaymentReconciliationStatus.Matched;
    }

    if (receiptDocumentId.HasValue)
    {
      receiptLinks.Add(PaymentReceiptLink.Create(
        EntityId.New(),
        OrganizationId,
        Id,
        receiptDocumentId.Value,
        "Recibo",
        createdAt,
        createdByUserId));
    }

    RecalculateStoredStatus(createdAt, createdByUserId);
    return transaction;
  }

  public void ReverseTransaction(EntityId transactionId, DateTimeOffset reversedAt, UserId? reversedByUserId, string? notes)
  {
    var transaction = transactions.FirstOrDefault(candidate => candidate.Id == transactionId) ??
      throw new InvalidOperationException("Payment transaction was not found.");

    transaction.Reverse(reversedAt, reversedByUserId, notes);
    RecalculateStoredStatus(reversedAt, reversedByUserId);
  }

  public void SetProviderInstruction(
    string providerCode,
    string providerReference,
    string metadataJson,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    EnsureCanMutate();
    ProviderCode = PaymentCode.Optional(providerCode, 80, nameof(providerCode));
    ProviderReference = PaymentCode.Optional(providerReference, 160, nameof(providerReference));
    ProviderMetadataJson = PaymentCode.Optional(metadataJson, 8000, nameof(metadataJson));
    ReconciliationStatus = PaymentReconciliationStatus.Pending;
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Cancel(DateTimeOffset updatedAt, UserId? updatedByUserId, string? notes = null)
  {
    if (Status is PaymentStatus.Archived or PaymentStatus.Cancelled)
    {
      return;
    }

    Status = PaymentStatus.Cancelled;
    Notes = PaymentCode.Optional(notes, 1000, nameof(notes)) ?? Notes;
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void MarkDisputed(DateTimeOffset updatedAt, UserId? updatedByUserId, string? notes = null)
  {
    if (Status is PaymentStatus.Archived or PaymentStatus.Cancelled)
    {
      throw new InvalidOperationException("Cancelled or archived charges cannot be disputed.");
    }

    Status = PaymentStatus.Disputed;
    Notes = PaymentCode.Optional(notes, 1000, nameof(notes)) ?? Notes;
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Archive(DateTimeOffset deletedAt, UserId? deletedByUserId)
  {
    if (IsDeleted)
    {
      return;
    }

    Status = PaymentStatus.Archived;
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
    Status = PaymentStatus.Pending;
    RecalculateStoredStatus(restoredAt, restoredByUserId);
  }

  public Money GrossAmount() => Amount.Add(PenaltyAmount).Subtract(DiscountAmount);

  public Money SettledAmount()
  {
    var settled = Money.Zero(Amount.Currency);
    foreach (var transaction in transactions.Where(transaction => !transaction.IsReversed))
    {
      settled = settled.Add(transaction.Amount);
    }

    return settled;
  }

  public Money Balance() => GrossAmount().Subtract(SettledAmount());

  public PaymentStatus GetEffectiveStatus(DateOnly today)
  {
    if (IsDeleted || Status == PaymentStatus.Archived)
    {
      return PaymentStatus.Archived;
    }

    if (Status is PaymentStatus.Cancelled or PaymentStatus.Disputed)
    {
      return Status;
    }

    var balance = Balance().Amount;
    if (balance <= 0)
    {
      return PaymentStatus.Paid;
    }

    if (SettledAmount().Amount > 0)
    {
      return PaymentStatus.PartiallyPaid;
    }

    return DueDate < today ? PaymentStatus.Overdue : PaymentStatus.Pending;
  }

  private void ApplyMetadata(
    EntityId? contractId,
    EntityId? propertyId,
    EntityId? residentId,
    EntityId? utilityAccountId,
    string title,
    string? description,
    DateOnly dueDate,
    Money amount,
    Money? discountAmount,
    Money? penaltyAmount,
    PaymentMethod preferredMethod,
    PaymentReconciliationStatus reconciliationStatus,
    string? notes,
    string? contractSearchText,
    string? propertySearchText,
    string? residentSearchText)
  {
    ArgumentNullException.ThrowIfNull(amount);
    if (amount.Amount <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(amount), "Charge amount must be positive.");
    }

    ContractId = contractId;
    PropertyId = propertyId;
    ResidentId = residentId;
    UtilityAccountId = utilityAccountId;
    Title = PaymentCode.Optional(title, 200, nameof(title)) ??
      throw new ArgumentException("Charge title is required.", nameof(title));
    Description = PaymentCode.Optional(description, 1000, nameof(description));
    DueDate = dueDate == default ? throw new ArgumentException("Due date is required.", nameof(dueDate)) : dueDate;
    Amount = amount;
    DiscountAmount = NormalizeAdjustment(discountAmount, amount.Currency, nameof(discountAmount));
    PenaltyAmount = NormalizeAdjustment(penaltyAmount, amount.Currency, nameof(penaltyAmount));
    if (GrossAmount().Amount <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(discountAmount), "Charge gross amount must remain positive.");
    }

    PreferredMethod = preferredMethod;
    ReconciliationStatus = reconciliationStatus;
    Notes = PaymentCode.Optional(notes, 1000, nameof(notes));
    SearchText = PaymentCode.NormalizeSearchText(
      Id.Value.ToString("D"),
      ContractId?.Value.ToString("D"),
      PropertyId?.Value.ToString("D"),
      ResidentId?.Value.ToString("D"),
      Title,
      Description,
      contractSearchText,
      propertySearchText,
      residentSearchText,
      Notes);
  }

  private void RecalculateStoredStatus(DateTimeOffset changedAt, UserId? changedByUserId)
  {
    Status = GetEffectiveStatus(DateOnly.FromDateTime(changedAt.UtcDateTime));
    MarkUpdated(changedAt, changedByUserId);
  }

  private void EnsureCanMutate()
  {
    if (Status is PaymentStatus.Archived or PaymentStatus.Cancelled)
    {
      throw new InvalidOperationException("Charge cannot be changed from its current status.");
    }
  }

  private void EnsureCanSettle()
  {
    if (Status is PaymentStatus.Archived or PaymentStatus.Cancelled or PaymentStatus.Disputed)
    {
      throw new InvalidOperationException("Charge cannot be settled from its current status.");
    }
  }

  private static Money NormalizeAdjustment(Money? value, string currency, string parameterName)
  {
    if (value is null)
    {
      return Money.Zero(currency);
    }

    if (!StringComparer.Ordinal.Equals(value.Currency, currency))
    {
      throw new InvalidOperationException("Charge adjustment currency must match amount currency.");
    }

    if (value.Amount < 0)
    {
      throw new ArgumentOutOfRangeException(parameterName, "Charge adjustment cannot be negative.");
    }

    return value;
  }
}
