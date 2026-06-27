using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Inspections;

public sealed class Inspection : TenantScopedEntity<EntityId>
{
  private readonly List<InspectionChecklistItem> checklistItems = [];
  private readonly List<InspectionDocumentLink> documentLinks = [];
  private readonly List<InspectionSignatureSlot> signatureSlots = [];

  private Inspection()
  {
  }

  private Inspection(
    EntityId id,
    OrganizationId organizationId,
    InspectionType type,
    EntityId propertyId,
    EntityId? contractId,
    EntityId? residentId,
    DateTimeOffset scheduledAt,
    UserId assignedUserId,
    string? assignedUserName,
    string? title,
    string? notes,
    string? propertySearchText,
    string? contractSearchText,
    string? residentSearchText,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    ApplySchedule(
      type,
      propertyId,
      contractId,
      residentId,
      scheduledAt,
      assignedUserId,
      assignedUserName,
      title,
      notes,
      propertySearchText,
      contractSearchText,
      residentSearchText);
    Status = InspectionStatus.Scheduled;
  }

  public InspectionType Type { get; private set; }

  public EntityId PropertyId { get; private set; }

  public EntityId? ContractId { get; private set; }

  public EntityId? ResidentId { get; private set; }

  public DateTimeOffset ScheduledAt { get; private set; }

  public UserId AssignedUserId { get; private set; }

  public string? AssignedUserName { get; private set; }

  public InspectionStatus Status { get; private set; }

  public string Title { get; private set; } = string.Empty;

  public string? Notes { get; private set; }

  public DateTimeOffset? StartedAt { get; private set; }

  public UserId? StartedByUserId { get; private set; }

  public DateTimeOffset? CompletedAt { get; private set; }

  public UserId? CompletedByUserId { get; private set; }

  public string? CompletionNotes { get; private set; }

  public DateTimeOffset? CancelledAt { get; private set; }

  public UserId? CancelledByUserId { get; private set; }

  public string? CancellationReason { get; private set; }

  public string SearchText { get; private set; } = string.Empty;

  public IReadOnlyCollection<InspectionChecklistItem> ChecklistItems => checklistItems.AsReadOnly();

  public IReadOnlyCollection<InspectionDocumentLink> DocumentLinks => documentLinks.AsReadOnly();

  public IReadOnlyCollection<InspectionSignatureSlot> SignatureSlots => signatureSlots.AsReadOnly();

  public int ActiveChecklistItemCount => checklistItems.Count(item => !item.IsDeleted);

  public int CompletedChecklistItemCount =>
    checklistItems.Count(item => !item.IsDeleted && item.IsComplete);

  public decimal CompletionPercentage =>
    ActiveChecklistItemCount == 0
      ? 0m
      : Math.Round(CompletedChecklistItemCount * 100m / ActiveChecklistItemCount, 2);

  public static Inspection Create(
    EntityId id,
    OrganizationId organizationId,
    InspectionType type,
    EntityId propertyId,
    EntityId? contractId,
    EntityId? residentId,
    DateTimeOffset scheduledAt,
    UserId assignedUserId,
    string? assignedUserName,
    string? title,
    string? notes,
    string? propertySearchText,
    string? contractSearchText,
    string? residentSearchText,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(
      id,
      organizationId,
      type,
      propertyId,
      contractId,
      residentId,
      scheduledAt,
      assignedUserId,
      assignedUserName,
      title,
      notes,
      propertySearchText,
      contractSearchText,
      residentSearchText,
      createdAt,
      createdByUserId);

  public void UpdateSchedule(
    InspectionType type,
    EntityId propertyId,
    EntityId? contractId,
    EntityId? residentId,
    DateTimeOffset scheduledAt,
    UserId assignedUserId,
    string? assignedUserName,
    string? title,
    string? notes,
    string? propertySearchText,
    string? contractSearchText,
    string? residentSearchText,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    EnsureCanEdit();
    ApplySchedule(
      type,
      propertyId,
      contractId,
      residentId,
      scheduledAt,
      assignedUserId,
      assignedUserName,
      title,
      notes,
      propertySearchText,
      contractSearchText,
      residentSearchText);
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public InspectionChecklistItem AddChecklistItem(
    string areaName,
    string itemName,
    bool isRequired,
    InspectionConditionRating conditionRating,
    string? observations,
    int sortOrder,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
  {
    EnsureCanEditChecklist();
    var item = InspectionChecklistItem.Create(
      EntityId.New(),
      OrganizationId,
      Id,
      areaName,
      itemName,
      isRequired,
      conditionRating,
      observations,
      sortOrder,
      createdAt,
      createdByUserId);
    checklistItems.Add(item);
    MarkUpdated(createdAt, createdByUserId);
    return item;
  }

  public void UpdateChecklistItem(
    EntityId checklistItemId,
    string areaName,
    string itemName,
    bool isRequired,
    InspectionConditionRating conditionRating,
    string? observations,
    int sortOrder,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    EnsureCanEditChecklist();
    var item = FindActiveChecklistItem(checklistItemId);
    item.Update(areaName, itemName, isRequired, conditionRating, observations, sortOrder, updatedAt, updatedByUserId);
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void RemoveChecklistItem(
    EntityId checklistItemId,
    DateTimeOffset deletedAt,
    UserId? deletedByUserId)
  {
    EnsureCanEditChecklist();
    var item = FindActiveChecklistItem(checklistItemId);
    item.Remove(deletedAt, deletedByUserId);
    foreach (var link in documentLinks.Where(link => link.ChecklistItemId == checklistItemId && !link.IsDeleted))
    {
      link.Remove(deletedAt, deletedByUserId);
    }

    MarkUpdated(deletedAt, deletedByUserId);
  }

  public void LinkDocument(
    EntityId documentId,
    InspectionDocumentKind kind,
    EntityId? checklistItemId,
    string? label,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
  {
    EnsureCanEditChecklist();
    if (checklistItemId.HasValue)
    {
      _ = FindActiveChecklistItem(checklistItemId.Value);
    }

    if (documentLinks.Any(link =>
      link.DocumentId == documentId &&
      link.Kind == kind &&
      link.ChecklistItemId == checklistItemId &&
      !link.IsDeleted))
    {
      return;
    }

    documentLinks.Add(InspectionDocumentLink.Create(
      EntityId.New(),
      OrganizationId,
      Id,
      checklistItemId,
      documentId,
      kind,
      label,
      createdAt,
      createdByUserId));
    MarkUpdated(createdAt, createdByUserId);
  }

  public void ReplaceSignatureSlots(
    IEnumerable<InspectionSignatureSlotDraft> slots,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    ArgumentNullException.ThrowIfNull(slots);

    EnsureCanEdit();
    foreach (var slot in signatureSlots.Where(slot => !slot.IsDeleted))
    {
      slot.Remove(updatedAt, updatedByUserId);
    }

    foreach (var slot in slots)
    {
      signatureSlots.Add(InspectionSignatureSlot.Create(
        EntityId.New(),
        OrganizationId,
        Id,
        slot.SignerRole,
        slot.SignerName,
        slot.IsRequired,
        updatedAt,
        updatedByUserId));
    }

    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Start(DateTimeOffset startedAt, UserId? startedByUserId)
  {
    EnsureCanEdit();
    if (Status == InspectionStatus.InProgress)
    {
      return;
    }

    Status = InspectionStatus.InProgress;
    StartedAt ??= startedAt;
    StartedByUserId ??= startedByUserId;
    SearchText = BuildSearchText(null, null, null);
    MarkUpdated(startedAt, startedByUserId);
  }

  public void Complete(string? completionNotes, DateTimeOffset completedAt, UserId? completedByUserId)
  {
    if (Status == InspectionStatus.Completed)
    {
      return;
    }

    EnsureCanEdit();
    if (ActiveChecklistItemCount == 0 || checklistItems.Any(item => !item.IsDeleted && !item.IsComplete))
    {
      throw new InvalidOperationException("Inspection cannot be completed until required checklist items are rated.");
    }

    if (signatureSlots.Any(slot => !slot.IsDeleted && slot.IsRequired && !slot.IsSigned))
    {
      throw new InvalidOperationException("Inspection cannot be completed until required signatures are signed.");
    }

    Status = InspectionStatus.Completed;
    StartedAt ??= completedAt;
    StartedByUserId ??= completedByUserId;
    CompletedAt = completedAt;
    CompletedByUserId = completedByUserId;
    CompletionNotes = InspectionCode.Optional(completionNotes, 2000, nameof(completionNotes));
    SearchText = BuildSearchText(null, null, null);
    MarkUpdated(completedAt, completedByUserId);
  }

  public void Cancel(string? reason, DateTimeOffset cancelledAt, UserId? cancelledByUserId)
  {
    EnsureCanEdit();
    Status = InspectionStatus.Cancelled;
    CancelledAt = cancelledAt;
    CancelledByUserId = cancelledByUserId;
    CancellationReason = InspectionCode.Optional(reason, 1000, nameof(reason));
    SearchText = BuildSearchText(null, null, null);
    MarkUpdated(cancelledAt, cancelledByUserId);
  }

  public void Archive(DateTimeOffset deletedAt, UserId? deletedByUserId)
  {
    if (IsDeleted)
    {
      return;
    }

    Status = InspectionStatus.Archived;
    SearchText = BuildSearchText(null, null, null);
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
    if (Status == InspectionStatus.Archived)
    {
      Status = InspectionStatus.Scheduled;
    }

    SearchText = BuildSearchText(null, null, null);
    MarkUpdated(restoredAt, restoredByUserId);
  }

  private void ApplySchedule(
    InspectionType type,
    EntityId propertyId,
    EntityId? contractId,
    EntityId? residentId,
    DateTimeOffset scheduledAt,
    UserId assignedUserId,
    string? assignedUserName,
    string? title,
    string? notes,
    string? propertySearchText,
    string? contractSearchText,
    string? residentSearchText)
  {
    if (propertyId.Value == Guid.Empty)
    {
      throw new ArgumentException("Property is required.", nameof(propertyId));
    }

    if (assignedUserId.Value == Guid.Empty)
    {
      throw new ArgumentException("Assignee is required.", nameof(assignedUserId));
    }

    if (scheduledAt == default)
    {
      throw new ArgumentException("Scheduled date is required.", nameof(scheduledAt));
    }

    Type = type;
    PropertyId = propertyId;
    ContractId = contractId;
    ResidentId = residentId;
    ScheduledAt = scheduledAt;
    AssignedUserId = assignedUserId;
    AssignedUserName = InspectionCode.Optional(assignedUserName, 160, nameof(assignedUserName));
    Title = string.IsNullOrWhiteSpace(title)
      ? $"Inspection {scheduledAt:yyyy-MM-dd}"
      : InspectionCode.Required(title, 200, nameof(title));
    Notes = InspectionCode.Optional(notes, 2000, nameof(notes));
    SearchText = BuildSearchText(propertySearchText, contractSearchText, residentSearchText);
  }

  private string BuildSearchText(
    string? propertySearchText,
    string? contractSearchText,
    string? residentSearchText) =>
    InspectionCode.NormalizeSearchText(
      Id.Value.ToString("D"),
      PropertyId.Value.ToString("D"),
      ContractId?.Value.ToString("D"),
      ResidentId?.Value.ToString("D"),
      AssignedUserId.Value.ToString("D"),
      AssignedUserName,
      Type.ToString(),
      Status.ToString(),
      Title,
      Notes,
      CompletionNotes,
      CancellationReason,
      propertySearchText,
      contractSearchText,
      residentSearchText);

  private InspectionChecklistItem FindActiveChecklistItem(EntityId itemId) =>
    checklistItems.FirstOrDefault(item => item.Id == itemId && !item.IsDeleted) ??
    throw new InvalidOperationException("Inspection checklist item was not found.");

  private void EnsureCanEdit()
  {
    if (IsDeleted || Status is InspectionStatus.Archived or InspectionStatus.Completed or InspectionStatus.Cancelled)
    {
      throw new InvalidOperationException("Completed, cancelled, or archived inspections cannot be changed.");
    }
  }

  private void EnsureCanEditChecklist()
  {
    if (IsDeleted || Status is InspectionStatus.Archived or InspectionStatus.Completed or InspectionStatus.Cancelled)
    {
      throw new InvalidOperationException("Checklist data is locked for this inspection.");
    }
  }
}

public sealed record InspectionSignatureSlotDraft(
  string SignerRole,
  string? SignerName,
  bool IsRequired);
