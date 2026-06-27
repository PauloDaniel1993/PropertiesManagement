using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Documents;

public sealed class DocumentVersion : TenantScopedEntity<EntityId>
{
  private DocumentVersion()
  {
  }

  private DocumentVersion(
    EntityId id,
    OrganizationId organizationId,
    EntityId documentId,
    int versionNumber,
    string fileName,
    string contentType,
    long sizeBytes,
    string storageKey,
    string? notes,
    DateTimeOffset uploadedAt,
    UserId? uploadedByUserId)
    : base(id, organizationId, uploadedAt, uploadedByUserId)
  {
    DocumentId = documentId;
    VersionNumber = versionNumber > 0
      ? versionNumber
      : throw new ArgumentOutOfRangeException(nameof(versionNumber), "Version number must be positive.");
    FileName = DocumentCode.Required(fileName, nameof(fileName), 180);
    ContentType = DocumentCode.Required(contentType, nameof(contentType), 140);
    SizeBytes = sizeBytes > 0
      ? sizeBytes
      : throw new ArgumentOutOfRangeException(nameof(sizeBytes), "File size must be positive.");
    StorageKey = DocumentCode.Required(storageKey, nameof(storageKey), 600);
    Notes = DocumentCode.Optional(notes, 500, nameof(notes));
  }

  public EntityId DocumentId { get; private set; }

  public int VersionNumber { get; private set; }

  public string FileName { get; private set; } = string.Empty;

  public string ContentType { get; private set; } = string.Empty;

  public long SizeBytes { get; private set; }

  public string StorageKey { get; private set; } = string.Empty;

  public string? Notes { get; private set; }

  public static DocumentVersion Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId documentId,
    int versionNumber,
    string fileName,
    string contentType,
    long sizeBytes,
    string storageKey,
    string? notes,
    DateTimeOffset uploadedAt,
    UserId? uploadedByUserId = null) =>
    new(
      id,
      organizationId,
      documentId,
      versionNumber,
      fileName,
      contentType,
      sizeBytes,
      storageKey,
      notes,
      uploadedAt,
      uploadedByUserId);
}
