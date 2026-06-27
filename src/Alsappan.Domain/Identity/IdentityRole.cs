using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Identity;

public sealed class IdentityRole : TenantScopedEntity<EntityId>
{
  private IdentityRole()
  {
  }

  private IdentityRole(
    EntityId id,
    OrganizationId organizationId,
    string code,
    string displayName,
    DateTimeOffset createdAt,
    UserId? createdByUserId,
    string? description,
    bool isSystem,
    bool isAssignable)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    Code = IdentityCode.NormalizeCode(code);
    DisplayName = IdentityCode.Required(displayName, nameof(displayName));
    Description = IdentityCode.Optional(description);
    IsSystem = isSystem;
    IsAssignable = isAssignable;
  }

  public string Code { get; private set; } = string.Empty;

  public string DisplayName { get; private set; } = string.Empty;

  public string? Description { get; private set; }

  public bool IsSystem { get; private set; }

  public bool IsAssignable { get; private set; }

  public static IdentityRole Create(
    EntityId id,
    OrganizationId organizationId,
    string code,
    string displayName,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null,
    string? description = null,
    bool isSystem = true,
    bool isAssignable = true) =>
    new(id, organizationId, code, displayName, createdAt, createdByUserId, description, isSystem, isAssignable);

  public void Rename(string displayName, string? description, DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    DisplayName = IdentityCode.Required(displayName, nameof(displayName));
    Description = IdentityCode.Optional(description);
    MarkUpdated(updatedAt, updatedByUserId);
  }
}
