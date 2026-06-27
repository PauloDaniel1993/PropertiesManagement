using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Occurrences;

public sealed class Occurrence : TenantScopedEntity<EntityId>
{
  private readonly List<OccurrenceComment> comments = [];
  private readonly List<OccurrenceAttachment> attachments = [];
  private readonly List<OccurrenceStatusHistory> statusHistory = [];
  private readonly List<OccurrencePriorityHistory> priorityHistory = [];
  private readonly List<OccurrenceAssignmentHistory> assignmentHistory = [];

  private Occurrence()
  {
  }

  private Occurrence(
    EntityId id,
    OrganizationId organizationId,
    string title,
    string description,
    OccurrenceType type,
    OccurrencePriority priority,
    EntityId? propertyId,
    EntityId? residentId,
    EntityId? contractId,
    UserId? assignedUserId,
    DateOnly? dueDate,
    string? propertySearchText,
    string? residentSearchText,
    string? contractSearchText,
    string? assigneeSearchText,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    Type = type;
    Priority = priority;
    Status = assignedUserId.HasValue ? OccurrenceStatus.Assigned : OccurrenceStatus.Open;
    ApplyDetails(
      title,
      description,
      type,
      propertyId,
      residentId,
      contractId,
      assignedUserId,
      dueDate,
      propertySearchText,
      residentSearchText,
      contractSearchText,
      assigneeSearchText);
    statusHistory.Add(OccurrenceStatusHistory.Create(
      EntityId.New(),
      organizationId,
      id,
      null,
      Status,
      "Initial status",
      createdAt,
      createdByUserId));
    priorityHistory.Add(OccurrencePriorityHistory.Create(
      EntityId.New(),
      organizationId,
      id,
      null,
      Priority,
      "Initial priority",
      createdAt,
      createdByUserId));
    if (assignedUserId.HasValue)
    {
      assignmentHistory.Add(OccurrenceAssignmentHistory.Create(
        EntityId.New(),
        organizationId,
        id,
        null,
        assignedUserId,
        "Initial assignment",
        createdAt,
        createdByUserId));
    }
  }

  public string Title { get; private set; } = string.Empty;

  public string Description { get; private set; } = string.Empty;

  public OccurrenceType Type { get; private set; }

  public OccurrencePriority Priority { get; private set; }

  public OccurrenceStatus Status { get; private set; }

  public EntityId? PropertyId { get; private set; }

  public EntityId? ResidentId { get; private set; }

  public EntityId? ContractId { get; private set; }

  public UserId? AssignedUserId { get; private set; }

  public DateOnly? DueDate { get; private set; }

  public DateTimeOffset? ResolvedAt { get; private set; }

  public UserId? ResolvedByUserId { get; private set; }

  public string? ResolutionNotes { get; private set; }

  public DateTimeOffset? CancelledAt { get; private set; }

  public UserId? CancelledByUserId { get; private set; }

  public string? CancellationNotes { get; private set; }

  public string SearchText { get; private set; } = string.Empty;

  public IReadOnlyCollection<OccurrenceComment> Comments => comments.AsReadOnly();

  public IReadOnlyCollection<OccurrenceAttachment> Attachments => attachments.AsReadOnly();

  public IReadOnlyCollection<OccurrenceStatusHistory> StatusHistory => statusHistory.AsReadOnly();

  public IReadOnlyCollection<OccurrencePriorityHistory> PriorityHistory => priorityHistory.AsReadOnly();

  public IReadOnlyCollection<OccurrenceAssignmentHistory> AssignmentHistory => assignmentHistory.AsReadOnly();

  public bool IsUnresolved => Status is not OccurrenceStatus.Resolved
    and not OccurrenceStatus.Cancelled
    and not OccurrenceStatus.Archived;

  public static Occurrence Create(
    EntityId id,
    OrganizationId organizationId,
    string title,
    string description,
    OccurrenceType type,
    OccurrencePriority priority,
    EntityId? propertyId,
    EntityId? residentId,
    EntityId? contractId,
    UserId? assignedUserId,
    DateOnly? dueDate,
    string? propertySearchText,
    string? residentSearchText,
    string? contractSearchText,
    string? assigneeSearchText,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(
      id,
      organizationId,
      title,
      description,
      type,
      priority,
      propertyId,
      residentId,
      contractId,
      assignedUserId,
      dueDate,
      propertySearchText,
      residentSearchText,
      contractSearchText,
      assigneeSearchText,
      createdAt,
      createdByUserId);

  public void UpdateDetails(
    string title,
    string description,
    OccurrenceType type,
    EntityId? propertyId,
    EntityId? residentId,
    EntityId? contractId,
    DateOnly? dueDate,
    string? propertySearchText,
    string? residentSearchText,
    string? contractSearchText,
    string? assigneeSearchText,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    EnsureCanMutate();
    ApplyDetails(
      title,
      description,
      type,
      propertyId,
      residentId,
      contractId,
      AssignedUserId,
      dueDate,
      propertySearchText,
      residentSearchText,
      contractSearchText,
      assigneeSearchText);
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Assign(
    UserId? assignedUserId,
    string? assigneeSearchText,
    string? notes,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    EnsureCanMutate();
    if (AssignedUserId == assignedUserId)
    {
      return;
    }

    var previous = AssignedUserId;
    AssignedUserId = assignedUserId;
    assignmentHistory.Add(OccurrenceAssignmentHistory.Create(
      EntityId.New(),
      OrganizationId,
      Id,
      previous,
      AssignedUserId,
      notes,
      updatedAt,
      updatedByUserId));

    if (assignedUserId.HasValue && Status == OccurrenceStatus.Open)
    {
      AddStatusHistory(Status, OccurrenceStatus.Assigned, notes, updatedAt, updatedByUserId);
      Status = OccurrenceStatus.Assigned;
    }

    SearchText = BuildSearchText(null, null, null, assigneeSearchText);
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void ChangePriority(
    OccurrencePriority priority,
    string? notes,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    EnsureCanMutate();
    if (Priority == priority)
    {
      return;
    }

    priorityHistory.Add(OccurrencePriorityHistory.Create(
      EntityId.New(),
      OrganizationId,
      Id,
      Priority,
      priority,
      notes,
      updatedAt,
      updatedByUserId));
    Priority = priority;
    SearchText = BuildSearchText();
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void ChangeStatus(
    OccurrenceStatus status,
    string? notes,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    EnsureCanMutate();
    if (status is OccurrenceStatus.Archived or OccurrenceStatus.Resolved or OccurrenceStatus.Cancelled)
    {
      throw new InvalidOperationException("Use the dedicated lifecycle operation for this status.");
    }

    if (Status == status)
    {
      return;
    }

    AddStatusHistory(Status, status, notes, updatedAt, updatedByUserId);
    Status = status;
    SearchText = BuildSearchText();
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Resolve(
    string resolutionNotes,
    DateTimeOffset resolvedAt,
    UserId? resolvedByUserId)
  {
    EnsureCanMutate();
    var notes = OccurrenceCode.Required(resolutionNotes, 2000, nameof(resolutionNotes));
    ResolutionNotes = notes;
    ResolvedAt = resolvedAt;
    ResolvedByUserId = resolvedByUserId;
    AddStatusHistory(Status, OccurrenceStatus.Resolved, notes, resolvedAt, resolvedByUserId);
    Status = OccurrenceStatus.Resolved;
    SearchText = BuildSearchText();
    MarkUpdated(resolvedAt, resolvedByUserId);
  }

  public void Cancel(
    string? notes,
    DateTimeOffset cancelledAt,
    UserId? cancelledByUserId)
  {
    EnsureCanMutate();
    CancellationNotes = OccurrenceCode.Optional(notes, 1000, nameof(notes));
    CancelledAt = cancelledAt;
    CancelledByUserId = cancelledByUserId;
    AddStatusHistory(Status, OccurrenceStatus.Cancelled, CancellationNotes, cancelledAt, cancelledByUserId);
    Status = OccurrenceStatus.Cancelled;
    SearchText = BuildSearchText();
    MarkUpdated(cancelledAt, cancelledByUserId);
  }

  public void AddComment(
    string body,
    bool isInternal,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
  {
    EnsureNotArchived();
    comments.Add(OccurrenceComment.Create(
      EntityId.New(),
      OrganizationId,
      Id,
      body,
      isInternal,
      createdAt,
      createdByUserId));
    MarkUpdated(createdAt, createdByUserId);
  }

  public void LinkDocument(
    EntityId documentId,
    string? label,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
  {
    EnsureNotArchived();
    if (attachments.Any(link => link.DocumentId == documentId && !link.IsDeleted))
    {
      return;
    }

    attachments.Add(OccurrenceAttachment.Create(
      EntityId.New(),
      OrganizationId,
      Id,
      documentId,
      label,
      createdAt,
      createdByUserId));
    MarkUpdated(createdAt, createdByUserId);
  }

  public void Archive(DateTimeOffset deletedAt, UserId? deletedByUserId)
  {
    if (IsDeleted)
    {
      return;
    }

    AddStatusHistory(Status, OccurrenceStatus.Archived, null, deletedAt, deletedByUserId);
    Status = OccurrenceStatus.Archived;
    SearchText = BuildSearchText();
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
    AddStatusHistory(Status, OccurrenceStatus.Open, null, restoredAt, restoredByUserId);
    Status = OccurrenceStatus.Open;
    SearchText = BuildSearchText();
    MarkUpdated(restoredAt, restoredByUserId);
  }

  public OccurrenceStatus EffectiveStatus =>
    IsDeleted ? OccurrenceStatus.Archived : Status;

  private void ApplyDetails(
    string title,
    string description,
    OccurrenceType type,
    EntityId? propertyId,
    EntityId? residentId,
    EntityId? contractId,
    UserId? assignedUserId,
    DateOnly? dueDate,
    string? propertySearchText,
    string? residentSearchText,
    string? contractSearchText,
    string? assigneeSearchText)
  {
    Title = OccurrenceCode.Required(title, 200, nameof(title));
    Description = OccurrenceCode.Required(description, 4000, nameof(description));
    Type = type;
    PropertyId = propertyId;
    ResidentId = residentId;
    ContractId = contractId;
    AssignedUserId = assignedUserId;
    DueDate = dueDate;
    SearchText = BuildSearchText(
      propertySearchText,
      residentSearchText,
      contractSearchText,
      assigneeSearchText);
  }

  private void AddStatusHistory(
    OccurrenceStatus previousStatus,
    OccurrenceStatus newStatus,
    string? notes,
    DateTimeOffset createdAt,
    UserId? createdByUserId) =>
    statusHistory.Add(OccurrenceStatusHistory.Create(
      EntityId.New(),
      OrganizationId,
      Id,
      previousStatus,
      newStatus,
      notes,
      createdAt,
      createdByUserId));

  private string BuildSearchText(
    string? propertySearchText = null,
    string? residentSearchText = null,
    string? contractSearchText = null,
    string? assigneeSearchText = null) =>
    OccurrenceCode.NormalizeSearchText(
      Id.Value.ToString("D"),
      PropertyId?.Value.ToString("D"),
      ResidentId?.Value.ToString("D"),
      ContractId?.Value.ToString("D"),
      AssignedUserId?.Value.ToString("D"),
      Title,
      Description,
      Type.ToString(),
      Priority.ToString(),
      Status.ToString(),
      ResolutionNotes,
      CancellationNotes,
      propertySearchText,
      residentSearchText,
      contractSearchText,
      assigneeSearchText);

  private void EnsureCanMutate()
  {
    if (Status is OccurrenceStatus.Archived or OccurrenceStatus.Resolved or OccurrenceStatus.Cancelled || IsDeleted)
    {
      throw new InvalidOperationException("Resolved, cancelled, or archived occurrences cannot be changed.");
    }
  }

  private void EnsureNotArchived()
  {
    if (Status == OccurrenceStatus.Archived || IsDeleted)
    {
      throw new InvalidOperationException("Archived occurrences cannot be changed.");
    }
  }
}
