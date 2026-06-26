using Alsappan.Application.Common.Auth;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Infrastructure.Auth;

public sealed class RefreshSession
{
  private RefreshSession()
  {
  }

  private RefreshSession(RefreshSessionRecord record)
  {
    Id = record.Id;
    UserId = record.UserId;
    TokenHash = record.TokenHash;
    CreatedAt = record.CreatedAt;
    ExpiresAt = record.ExpiresAt;
    ActiveOrganizationId = record.ActiveOrganizationId;
    UserAgent = record.UserAgent;
    IpAddress = record.IpAddress;
    RevokedAt = record.RevokedAt;
    RevokedReason = record.RevokedReason;
    ReplacedBySessionId = record.ReplacedBySessionId;
  }

  public EntityId Id { get; private set; }

  public UserId UserId { get; private set; }

  public string TokenHash { get; private set; } = string.Empty;

  public DateTimeOffset CreatedAt { get; private set; }

  public DateTimeOffset ExpiresAt { get; private set; }

  public OrganizationId? ActiveOrganizationId { get; private set; }

  public string? UserAgent { get; private set; }

  public string? IpAddress { get; private set; }

  public DateTimeOffset? RevokedAt { get; private set; }

  public string? RevokedReason { get; private set; }

  public EntityId? ReplacedBySessionId { get; private set; }

  public bool IsRevoked => RevokedAt.HasValue;

  public static RefreshSession FromRecord(RefreshSessionRecord record)
  {
    ArgumentNullException.ThrowIfNull(record);
    return new RefreshSession(record);
  }

  public RefreshSessionRecord ToRecord() =>
    new(
      Id,
      UserId,
      TokenHash,
      CreatedAt,
      ExpiresAt,
      ActiveOrganizationId,
      UserAgent,
      IpAddress,
      RevokedAt,
      RevokedReason,
      ReplacedBySessionId);

  public void Revoke(DateTimeOffset revokedAt, string? reason = null, EntityId? replacedBySessionId = null)
  {
    if (revokedAt == default)
    {
      throw new ArgumentException("Revoked timestamp is required.", nameof(revokedAt));
    }

    RevokedAt = revokedAt;
    RevokedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    ReplacedBySessionId = replacedBySessionId;
  }
}
