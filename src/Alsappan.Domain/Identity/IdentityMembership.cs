using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Identity;

#pragma warning disable CA1819

public sealed class IdentityMembership : TenantScopedEntity<EntityId>
{
  private IdentityMembership()
  {
  }

  private IdentityMembership(
    EntityId id,
    OrganizationId organizationId,
    UserId userId,
    IEnumerable<string> roleCodes,
    IEnumerable<string>? permissionCodes,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    if (userId.Value == Guid.Empty)
    {
      throw new ArgumentException("User id is required.", nameof(userId));
    }

    UserId = userId;
    RoleCodes = RequireAnyRole(roleCodes);
    PermissionCodes = IdentityCode.NormalizeCodes(permissionCodes);
    Status = IdentityMembershipStatus.Active;
  }

  public UserId UserId { get; private set; }

  public string[] RoleCodes { get; private set; } = [];

  public string[] PermissionCodes { get; private set; } = [];

  public IdentityMembershipStatus Status { get; private set; }

  public bool IsActive => Status == IdentityMembershipStatus.Active && !IsDeleted;

  public static IdentityMembership Create(
    EntityId id,
    OrganizationId organizationId,
    UserId userId,
    IEnumerable<string> roleCodes,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null,
    IEnumerable<string>? permissionCodes = null) =>
    new(id, organizationId, userId, roleCodes, permissionCodes, createdAt, createdByUserId);

  public void AssignAccess(
    IEnumerable<string> roleCodes,
    IEnumerable<string>? permissionCodes,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    RoleCodes = RequireAnyRole(roleCodes);
    PermissionCodes = IdentityCode.NormalizeCodes(permissionCodes);
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Deactivate(DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    if (Status == IdentityMembershipStatus.Archived)
    {
      return;
    }

    Status = IdentityMembershipStatus.Inactive;
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Reactivate(DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    if (IsDeleted || Status == IdentityMembershipStatus.Archived)
    {
      throw new InvalidOperationException("Archived memberships cannot be reactivated.");
    }

    Status = IdentityMembershipStatus.Active;
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Archive(DateTimeOffset deletedAt, UserId? deletedByUserId)
  {
    if (IsDeleted)
    {
      return;
    }

    Status = IdentityMembershipStatus.Archived;
    MarkDeleted(deletedAt, deletedByUserId);
  }

  private static string[] RequireAnyRole(IEnumerable<string> roleCodes)
  {
    var normalized = IdentityCode.NormalizeCodes(roleCodes);
    if (normalized.Length == 0)
    {
      throw new ArgumentException("At least one role is required.", nameof(roleCodes));
    }

    return normalized;
  }
}
