using Alsappan.Application.Common.Tenancy;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Common.Auth;

public sealed class AccessTokenDescriptor
{
  public AccessTokenDescriptor(
    UserId userId,
    IEnumerable<OrganizationMembership> memberships,
    string? email = null,
    string? displayName = null,
    EntityId? refreshSessionId = null,
    IEnumerable<string>? platformRoleCodes = null,
    IEnumerable<string>? platformPermissionCodes = null)
  {
    if (userId.Value == Guid.Empty)
    {
      throw new ArgumentException("User id is required.", nameof(userId));
    }

    UserId = userId;
    Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
    RefreshSessionId = refreshSessionId;
    Memberships = (memberships ?? throw new ArgumentNullException(nameof(memberships))).ToArray();
    PlatformRoleCodes = NormalizeCodes(platformRoleCodes);
    PlatformPermissionCodes = NormalizeCodes(platformPermissionCodes);
  }

  public UserId UserId { get; }

  public string? Email { get; }

  public string? DisplayName { get; }

  public EntityId? RefreshSessionId { get; }

  public IReadOnlyList<OrganizationMembership> Memberships { get; }

  public IReadOnlySet<string> PlatformRoleCodes { get; }

  public IReadOnlySet<string> PlatformPermissionCodes { get; }

  private static HashSet<string> NormalizeCodes(IEnumerable<string>? values)
  {
    var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var value in values ?? [])
    {
      if (!string.IsNullOrWhiteSpace(value))
      {
#pragma warning disable CA1308
        codes.Add(value.Trim().ToLowerInvariant());
#pragma warning restore CA1308
      }
    }

    return codes;
  }
}

public sealed record IssuedAccessToken(
  string Token,
  string TokenType,
  DateTimeOffset IssuedAt,
  DateTimeOffset ExpiresAt)
{
  public IssuedAccessToken(
    string token,
    DateTimeOffset issuedAt,
    DateTimeOffset expiresAt)
    : this(token, AuthTransportConstants.BearerScheme, issuedAt, expiresAt)
  {
  }
}

public sealed record RefreshTokenSecret
{
  public RefreshTokenSecret(string token, string tokenHash)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(token);
    ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

    Token = token;
    TokenHash = tokenHash;
  }

  public string Token { get; }

  public string TokenHash { get; }
}

public sealed record RefreshSessionRecord
{
  public RefreshSessionRecord(
    EntityId id,
    UserId userId,
    string tokenHash,
    DateTimeOffset createdAt,
    DateTimeOffset expiresAt,
    OrganizationId? activeOrganizationId = null,
    string? userAgent = null,
    string? ipAddress = null,
    DateTimeOffset? revokedAt = null,
    string? revokedReason = null,
    EntityId? replacedBySessionId = null)
  {
    if (id.Value == Guid.Empty)
    {
      throw new ArgumentException("Refresh session id is required.", nameof(id));
    }

    if (userId.Value == Guid.Empty)
    {
      throw new ArgumentException("User id is required.", nameof(userId));
    }

    ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

    if (createdAt == default)
    {
      throw new ArgumentException("Created timestamp is required.", nameof(createdAt));
    }

    if (expiresAt <= createdAt)
    {
      throw new ArgumentException("Refresh session expiry must be after creation.", nameof(expiresAt));
    }

    if (revokedAt.HasValue && revokedAt.Value < createdAt)
    {
      throw new ArgumentException("Revoked timestamp cannot be before creation.", nameof(revokedAt));
    }

    Id = id;
    UserId = userId;
    TokenHash = tokenHash.Trim();
    CreatedAt = createdAt;
    ExpiresAt = expiresAt;
    ActiveOrganizationId = activeOrganizationId;
    UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim();
    IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim();
    RevokedAt = revokedAt;
    RevokedReason = string.IsNullOrWhiteSpace(revokedReason) ? null : revokedReason.Trim();
    ReplacedBySessionId = replacedBySessionId;
  }

  public EntityId Id { get; }

  public UserId UserId { get; }

  public string TokenHash { get; }

  public DateTimeOffset CreatedAt { get; }

  public DateTimeOffset ExpiresAt { get; }

  public OrganizationId? ActiveOrganizationId { get; }

  public string? UserAgent { get; }

  public string? IpAddress { get; }

  public DateTimeOffset? RevokedAt { get; }

  public string? RevokedReason { get; }

  public EntityId? ReplacedBySessionId { get; }

  public bool IsRevoked => RevokedAt.HasValue;

  public bool IsExpired(DateTimeOffset timestamp) => timestamp >= ExpiresAt;

  public bool CanRefresh(DateTimeOffset timestamp) => !IsRevoked && !IsExpired(timestamp);
}
