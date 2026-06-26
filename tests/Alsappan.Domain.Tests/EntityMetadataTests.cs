using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;
using System.Globalization;

namespace Alsappan.Domain.Tests;

public sealed class EntityMetadataTests
{
  [Fact]
  public void TenantScopedEntityCapturesOrganizationAndCreationAudit()
  {
    var organizationId = OrganizationId.New();
    var userId = UserId.New();
    var createdAt = Timestamp("2026-06-01T10:00:00Z");

    var entity = new TestTenantEntity(EntityId.New(), organizationId, createdAt, userId);

    Assert.Equal(organizationId, entity.OrganizationId);
    Assert.Equal(createdAt, entity.CreatedAt);
    Assert.Equal(userId, entity.CreatedByUserId);
    Assert.False(entity.IsDeleted);
    Assert.False(string.IsNullOrWhiteSpace(entity.ConcurrencyToken.Value));
  }

  [Fact]
  public void TenantScopedEntityRefreshesConcurrencyTokenWhenTouched()
  {
    var entity = new TestTenantEntity(
      EntityId.New(),
      OrganizationId.New(),
      Timestamp("2026-06-01T10:00:00Z"),
      UserId.New());
    var originalToken = entity.ConcurrencyToken;
    var updatedBy = UserId.New();

    entity.Touch(Timestamp("2026-06-02T10:00:00Z"), updatedBy);

    Assert.Equal(updatedBy, entity.UpdatedByUserId);
    Assert.NotEqual(originalToken, entity.ConcurrencyToken);
  }

  [Fact]
  public void TenantScopedEntityMarksSoftDeletion()
  {
    var entity = new TestTenantEntity(
      EntityId.New(),
      OrganizationId.New(),
      Timestamp("2026-06-01T10:00:00Z"),
      UserId.New());
    var deletedBy = UserId.New();

    entity.Delete(Timestamp("2026-06-03T10:00:00Z"), deletedBy);

    Assert.True(entity.IsDeleted);
    Assert.Equal(deletedBy, entity.DeletedByUserId);
  }

  private sealed class TestTenantEntity : TenantScopedEntity<EntityId>
  {
    public TestTenantEntity(
      EntityId id,
      OrganizationId organizationId,
      DateTimeOffset createdAt,
      UserId createdByUserId)
      : base(id, organizationId, createdAt, createdByUserId)
    {
    }

    public void Touch(DateTimeOffset updatedAt, UserId updatedByUserId)
    {
      MarkUpdated(updatedAt, updatedByUserId);
    }

    public void Delete(DateTimeOffset deletedAt, UserId deletedByUserId)
    {
      MarkDeleted(deletedAt, deletedByUserId);
    }
  }

  private static DateTimeOffset Timestamp(string value) =>
    DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
}
