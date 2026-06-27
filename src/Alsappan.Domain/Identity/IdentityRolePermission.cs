using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Identity;

#pragma warning disable CA1711

public sealed class IdentityRolePermission : TenantScopedEntity<EntityId>
{
  private IdentityRolePermission()
  {
  }

  private IdentityRolePermission(
    EntityId id,
    OrganizationId organizationId,
    EntityId roleId,
    string permissionCode,
    DateTimeOffset createdAt,
    UserId? createdByUserId,
    EntityId? permissionId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    RoleId = roleId;
    PermissionId = permissionId;
    PermissionCode = IdentityCode.NormalizeCode(permissionCode);
  }

  public EntityId RoleId { get; private set; }

  public EntityId? PermissionId { get; private set; }

  public string PermissionCode { get; private set; } = string.Empty;

  public static IdentityRolePermission Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId roleId,
    string permissionCode,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null,
    EntityId? permissionId = null) =>
    new(id, organizationId, roleId, permissionCode, createdAt, createdByUserId, permissionId);
}
