namespace Alsappan.Application.Common.Contracts;

public sealed record AuditMetadataDto
{
  public AuditMetadataDto(
    DateTimeOffset createdAt,
    Guid? createdByUserId = null,
    DateTimeOffset? updatedAt = null,
    Guid? updatedByUserId = null,
    DateTimeOffset? deletedAt = null,
    Guid? deletedByUserId = null,
    string? concurrencyToken = null)
  {
    if (createdAt == default)
    {
      throw new ArgumentException("Created timestamp is required.", nameof(createdAt));
    }

    if (updatedAt.HasValue && updatedAt.Value < createdAt)
    {
      throw new ArgumentException("Updated timestamp cannot be before created timestamp.", nameof(updatedAt));
    }

    if (deletedAt.HasValue && deletedAt.Value < createdAt)
    {
      throw new ArgumentException("Deleted timestamp cannot be before created timestamp.", nameof(deletedAt));
    }

    CreatedAt = createdAt;
    CreatedByUserId = createdByUserId;
    UpdatedAt = updatedAt;
    UpdatedByUserId = updatedByUserId;
    DeletedAt = deletedAt;
    DeletedByUserId = deletedByUserId;
    ConcurrencyToken = string.IsNullOrWhiteSpace(concurrencyToken) ? null : concurrencyToken.Trim();
  }

  public DateTimeOffset CreatedAt { get; }

  public Guid? CreatedByUserId { get; }

  public DateTimeOffset? UpdatedAt { get; }

  public Guid? UpdatedByUserId { get; }

  public DateTimeOffset? DeletedAt { get; }

  public Guid? DeletedByUserId { get; }

  public string? ConcurrencyToken { get; }

  public bool IsDeleted => DeletedAt.HasValue;
}
