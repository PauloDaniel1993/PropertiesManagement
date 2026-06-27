using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Identity;

public sealed class UserInvitation :
  TenantScopedEntity<EntityId>
{
  private UserInvitation()
  {
  }

  public UserInvitation(
    EntityId id,
    OrganizationId organizationId,
    string email,
    string displayName,
    EntityId roleId,
    string tokenHash,
    DateTimeOffset expiresAt,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    if (expiresAt <= createdAt)
    {
      throw new ArgumentException("Invitation expiry must be after creation.", nameof(expiresAt));
    }

    Email = Required(email, nameof(email));
    NormalizedEmail = IdentityCode.NormalizeEmail(email);
    DisplayName = Required(displayName, nameof(displayName));
    RoleId = roleId;
    TokenHash = Required(tokenHash, nameof(tokenHash));
    ExpiresAt = expiresAt;
    Status = "pending";
  }

  public string Email { get; private set; } = string.Empty;

  public string NormalizedEmail { get; private set; } = string.Empty;

  public string DisplayName { get; private set; } = string.Empty;

  public EntityId RoleId { get; private set; }

  public string TokenHash { get; private set; } = string.Empty;

  public DateTimeOffset ExpiresAt { get; private set; }

  public string Status { get; private set; } = "pending";

  public DateTimeOffset? AcceptedAt { get; private set; }

  public UserId? AcceptedByUserId { get; private set; }

  public void Accept(DateTimeOffset acceptedAt, UserId acceptedByUserId)
  {
    AcceptedAt = acceptedAt == default
      ? throw new ArgumentException("Accepted timestamp is required.", nameof(acceptedAt))
      : acceptedAt;
    AcceptedByUserId = acceptedByUserId;
    Status = "accepted";
    RefreshConcurrencyToken();
  }

  public void Revoke(DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    Status = "revoked";
    MarkUpdated(updatedAt, updatedByUserId);
  }

  private static string Required(string value, string parameterName)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
    return value.Trim();
  }
}
