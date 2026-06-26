using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Domain.Common.Metadata;

public interface IOrganizationScoped
{
  OrganizationId OrganizationId { get; }
}

public interface ICreationAudited
{
  DateTimeOffset CreatedAt { get; }

  UserId? CreatedByUserId { get; }
}

public interface IModificationAudited
{
  DateTimeOffset? UpdatedAt { get; }

  UserId? UpdatedByUserId { get; }
}

public interface IDeletionAudited
{
  DateTimeOffset? DeletedAt { get; }

  UserId? DeletedByUserId { get; }
}

public interface ISoftDeletable : IDeletionAudited
{
  bool IsDeleted { get; }
}

public interface IConcurrencyTracked
{
  ConcurrencyToken ConcurrencyToken { get; }
}

public abstract class Entity<TId> : IConcurrencyTracked
  where TId : notnull
{
  protected Entity()
  {
    Id = default!;
  }

  protected Entity(TId id)
  {
    ArgumentNullException.ThrowIfNull(id);
    Id = id;
  }

  public TId Id { get; protected init; }

  public ConcurrencyToken ConcurrencyToken { get; protected set; } = ConcurrencyToken.New();

  protected void RefreshConcurrencyToken()
  {
    ConcurrencyToken = ConcurrencyToken.New();
  }
}

public abstract class TenantScopedEntity<TId> :
  Entity<TId>,
  IOrganizationScoped,
  ICreationAudited,
  IModificationAudited,
  ISoftDeletable
  where TId : notnull
{
  protected TenantScopedEntity()
  {
  }

  protected TenantScopedEntity(
    TId id,
    OrganizationId organizationId,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id)
  {
    if (createdAt == default)
    {
      throw new ArgumentException("Created timestamp is required.", nameof(createdAt));
    }

    OrganizationId = organizationId;
    CreatedAt = createdAt;
    CreatedByUserId = createdByUserId;
  }

  public OrganizationId OrganizationId { get; protected init; }

  public DateTimeOffset CreatedAt { get; protected init; }

  public UserId? CreatedByUserId { get; protected init; }

  public DateTimeOffset? UpdatedAt { get; protected set; }

  public UserId? UpdatedByUserId { get; protected set; }

  public DateTimeOffset? DeletedAt { get; protected set; }

  public UserId? DeletedByUserId { get; protected set; }

  public bool IsDeleted => DeletedAt.HasValue;

  protected void MarkUpdated(DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    if (updatedAt == default)
    {
      throw new ArgumentException("Updated timestamp is required.", nameof(updatedAt));
    }

    UpdatedAt = updatedAt;
    UpdatedByUserId = updatedByUserId;
    RefreshConcurrencyToken();
  }

  protected void MarkDeleted(DateTimeOffset deletedAt, UserId? deletedByUserId)
  {
    if (deletedAt == default)
    {
      throw new ArgumentException("Deleted timestamp is required.", nameof(deletedAt));
    }

    DeletedAt = deletedAt;
    DeletedByUserId = deletedByUserId;
    RefreshConcurrencyToken();
  }
}
