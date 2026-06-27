using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Documents;

public sealed class DocumentRecord : TenantScopedEntity<EntityId>
{
  private readonly List<DocumentLink> links = [];
  private readonly List<DocumentVersion> versions = [];

  private DocumentRecord()
  {
  }

  private DocumentRecord(
    EntityId id,
    OrganizationId organizationId,
    DocumentCategory category,
    string title,
    string? description,
    string fileName,
    string contentType,
    long sizeBytes,
    string storageKey,
    string? versionNotes,
    IEnumerable<DocumentLinkDraft> linkDrafts,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    Category = RequireCategory(category);
    Status = DocumentStatus.Active;
    Title = DocumentCode.Required(title, nameof(title), 180);
    Description = DocumentCode.Optional(description, 1000, nameof(description));
    AddVersion(fileName, contentType, sizeBytes, storageKey, versionNotes, createdAt, createdByUserId);
    ReplaceLinks(linkDrafts, createdAt, createdByUserId);
    RefreshSearchText();
  }

  public DocumentCategory Category { get; private set; }

  public DocumentStatus Status { get; private set; }

  public string Title { get; private set; } = string.Empty;

  public string? Description { get; private set; }

  public string CurrentFileName { get; private set; } = string.Empty;

  public string CurrentContentType { get; private set; } = string.Empty;

  public long CurrentSizeBytes { get; private set; }

  public string CurrentStorageKey { get; private set; } = string.Empty;

  public int CurrentVersionNumber { get; private set; }

  public DateTimeOffset CurrentUploadedAt { get; private set; }

  public UserId? CurrentUploadedByUserId { get; private set; }

  public string SearchText { get; private set; } = string.Empty;

  public IReadOnlyCollection<DocumentLink> Links => links.AsReadOnly();

  public IReadOnlyCollection<DocumentVersion> Versions => versions.AsReadOnly();

  public static DocumentRecord Create(
    EntityId id,
    OrganizationId organizationId,
    DocumentCategory category,
    string title,
    string? description,
    string fileName,
    string contentType,
    long sizeBytes,
    string storageKey,
    string? versionNotes,
    IEnumerable<DocumentLinkDraft> linkDrafts,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(
      id,
      organizationId,
      category,
      title,
      description,
      fileName,
      contentType,
      sizeBytes,
      storageKey,
      versionNotes,
      linkDrafts,
      createdAt,
      createdByUserId);

  public void UpdateMetadata(
    DocumentCategory category,
    string title,
    string? description,
    IEnumerable<DocumentLinkDraft> linkDrafts,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    EnsureActive();

    Category = RequireCategory(category);
    Title = DocumentCode.Required(title, nameof(title), 180);
    Description = DocumentCode.Optional(description, 1000, nameof(description));
    ReplaceLinks(linkDrafts, updatedAt, updatedByUserId);
    RefreshSearchText();
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public DocumentVersion AddVersion(
    string fileName,
    string contentType,
    long sizeBytes,
    string storageKey,
    string? notes,
    DateTimeOffset uploadedAt,
    UserId? uploadedByUserId)
  {
    if (Status == DocumentStatus.Archived)
    {
      throw new InvalidOperationException("Archived documents cannot receive new versions.");
    }

    CurrentVersionNumber++;
    CurrentFileName = DocumentCode.Required(fileName, nameof(fileName), 180);
    CurrentContentType = DocumentCode.Required(contentType, nameof(contentType), 140);
    CurrentSizeBytes = sizeBytes > 0
      ? sizeBytes
      : throw new ArgumentOutOfRangeException(nameof(sizeBytes), "File size must be positive.");
    CurrentStorageKey = DocumentCode.Required(storageKey, nameof(storageKey), 600);
    CurrentUploadedAt = uploadedAt == default
      ? throw new ArgumentException("Upload timestamp is required.", nameof(uploadedAt))
      : uploadedAt;
    CurrentUploadedByUserId = uploadedByUserId;

    var version = DocumentVersion.Create(
      EntityId.New(),
      OrganizationId,
      Id,
      CurrentVersionNumber,
      CurrentFileName,
      CurrentContentType,
      CurrentSizeBytes,
      CurrentStorageKey,
      notes,
      uploadedAt,
      uploadedByUserId);
    versions.Add(version);
    RefreshSearchText();
    MarkUpdated(uploadedAt, uploadedByUserId);

    return version;
  }

  public void Archive(DateTimeOffset deletedAt, UserId? deletedByUserId)
  {
    if (IsDeleted)
    {
      return;
    }

    Status = DocumentStatus.Archived;
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
    Status = DocumentStatus.Active;
    MarkUpdated(restoredAt, restoredByUserId);
  }

  private void ReplaceLinks(
    IEnumerable<DocumentLinkDraft> linkDrafts,
    DateTimeOffset changedAt,
    UserId? changedByUserId)
  {
    ArgumentNullException.ThrowIfNull(linkDrafts);

    links.Clear();
    var seen = new HashSet<string>(StringComparer.Ordinal);
    foreach (var draft in linkDrafts)
    {
      var entityType = DocumentCode.NormalizeCode(draft.EntityType);
      var key = $"{entityType}:{draft.EntityId.Value:D}";
      if (!seen.Add(key))
      {
        continue;
      }

      links.Add(DocumentLink.Create(
        EntityId.New(),
        OrganizationId,
        Id,
        entityType,
        draft.EntityId,
        draft.Label,
        changedAt,
        changedByUserId));
    }
  }

  private void RefreshSearchText() =>
    SearchText = DocumentCode.NormalizeSearchText(
      Title,
      Description,
      CurrentFileName,
      Category.ToString(),
      Status.ToString(),
      string.Join(' ', links.Select(link => $"{link.EntityType} {link.EntityId.Value:D} {link.Label}")));

  private void EnsureActive()
  {
    if (Status == DocumentStatus.Archived)
    {
      throw new InvalidOperationException("Archived documents cannot be changed.");
    }
  }

  private static DocumentCategory RequireCategory(DocumentCategory category) =>
    Enum.IsDefined(category) ? category : throw new ArgumentOutOfRangeException(nameof(category));
}

public sealed record DocumentLinkDraft(
  string EntityType,
  EntityId EntityId,
  string? Label);
