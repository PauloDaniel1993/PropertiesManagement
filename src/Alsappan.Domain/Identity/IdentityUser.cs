using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Identity;

public sealed class IdentityUser :
  Entity<UserId>,
  ICreationAudited,
  IModificationAudited,
  ISoftDeletable
{
  private IdentityUser()
  {
  }

  private IdentityUser(
    UserId id,
    string email,
    string? displayName,
    UserAccountType accountType,
    UserStatus status,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id)
  {
    if (createdAt == default)
    {
      throw new ArgumentException("Created timestamp is required.", nameof(createdAt));
    }

    Email = IdentityCode.Required(email, nameof(email));
    NormalizedEmail = IdentityCode.NormalizeEmail(email);
    DisplayName = IdentityCode.Optional(displayName);
    AccountType = accountType;
    Status = status;
    CreatedAt = createdAt;
    CreatedByUserId = createdByUserId;
  }

  public string Email { get; private set; } = string.Empty;

  public string NormalizedEmail { get; private set; } = string.Empty;

  public string? DisplayName { get; private set; }

  public UserAccountType AccountType { get; private set; }

  public UserStatus Status { get; private set; }

  public string? PasswordHash { get; private set; }

  public DateTimeOffset? PasswordChangedAt { get; private set; }

  public int FailedLoginCount { get; private set; }

  public DateTimeOffset? LockedUntil { get; private set; }

  public DateTimeOffset? LastLoginAt { get; private set; }

  public DateTimeOffset CreatedAt { get; private set; }

  public UserId? CreatedByUserId { get; private set; }

  public DateTimeOffset? UpdatedAt { get; private set; }

  public UserId? UpdatedByUserId { get; private set; }

  public DateTimeOffset? DeletedAt { get; private set; }

  public UserId? DeletedByUserId { get; private set; }

  public bool HasPassword => !string.IsNullOrWhiteSpace(PasswordHash);

  public bool IsDeleted => DeletedAt.HasValue;

  public static IdentityUser Create(
    UserId id,
    string email,
    string? displayName,
    UserAccountType accountType,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null,
    UserStatus status = UserStatus.Invited) =>
    new(id, email, displayName, accountType, status, createdAt, createdByUserId);

  public bool IsLockoutActive(DateTimeOffset timestamp) =>
    Status == UserStatus.Locked &&
    (!LockedUntil.HasValue || timestamp < LockedUntil.Value);

  public bool CanAttemptPasswordLogin(DateTimeOffset timestamp)
  {
    if (IsDeleted || !HasPassword)
    {
      return false;
    }

    if (Status == UserStatus.Locked && !IsLockoutActive(timestamp))
    {
      return true;
    }

    return Status == UserStatus.Active && !IsLockoutActive(timestamp);
  }

  public bool CanMaintainSession(DateTimeOffset timestamp) =>
    !IsDeleted &&
    Status == UserStatus.Active &&
    !IsLockoutActive(timestamp);

  public void ChangeProfile(string email, string? displayName, DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    Email = IdentityCode.Required(email, nameof(email));
    NormalizedEmail = IdentityCode.NormalizeEmail(email);
    DisplayName = IdentityCode.Optional(displayName);
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void ChangeDisplayName(string? displayName, DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    DisplayName = IdentityCode.Optional(displayName);
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void SetPasswordHash(string passwordHash, DateTimeOffset changedAt, UserId? changedByUserId)
  {
    PasswordHash = IdentityCode.Required(passwordHash, nameof(passwordHash));
    PasswordChangedAt = changedAt == default
      ? throw new ArgumentException("Password change timestamp is required.", nameof(changedAt))
      : changedAt;
    FailedLoginCount = 0;
    LockedUntil = null;

    if (Status is UserStatus.Invited or UserStatus.Locked)
    {
      Status = UserStatus.Active;
    }

    MarkUpdated(changedAt, changedByUserId);
  }

  public void RecordSuccessfulLogin(DateTimeOffset occurredAt)
  {
    if (occurredAt == default)
    {
      throw new ArgumentException("Login timestamp is required.", nameof(occurredAt));
    }

    FailedLoginCount = 0;
    LockedUntil = null;
    LastLoginAt = occurredAt;

    if (Status == UserStatus.Locked)
    {
      Status = UserStatus.Active;
    }

    MarkUpdated(occurredAt, Id);
  }

  public void RecordFailedLogin(
    DateTimeOffset occurredAt,
    int maxFailedAccessAttempts = IdentityDefaults.DefaultMaxFailedAccessAttempts,
    TimeSpan? lockoutDuration = null)
  {
    if (occurredAt == default)
    {
      throw new ArgumentException("Login timestamp is required.", nameof(occurredAt));
    }

    if (maxFailedAccessAttempts < 1)
    {
      throw new ArgumentOutOfRangeException(nameof(maxFailedAccessAttempts), "Failed access limit must be greater than zero.");
    }

    FailedLoginCount++;

    if (FailedLoginCount >= maxFailedAccessAttempts)
    {
      Status = UserStatus.Locked;
      LockedUntil = occurredAt.Add(lockoutDuration ?? IdentityDefaults.DefaultLockoutDuration);
    }

    MarkUpdated(occurredAt, null);
  }

  public void Deactivate(DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    if (Status == UserStatus.Archived)
    {
      return;
    }

    Status = UserStatus.Inactive;
    LockedUntil = null;
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Reactivate(DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    if (IsDeleted || Status == UserStatus.Archived)
    {
      throw new InvalidOperationException("Archived users cannot be reactivated.");
    }

    Status = UserStatus.Active;
    FailedLoginCount = 0;
    LockedUntil = null;
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Archive(DateTimeOffset deletedAt, UserId? deletedByUserId)
  {
    if (IsDeleted)
    {
      return;
    }

    Status = UserStatus.Archived;
    DeletedAt = deletedAt == default
      ? throw new ArgumentException("Deleted timestamp is required.", nameof(deletedAt))
      : deletedAt;
    DeletedByUserId = deletedByUserId;
    MarkUpdated(deletedAt, deletedByUserId);
  }

  private void MarkUpdated(DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    if (updatedAt == default)
    {
      throw new ArgumentException("Updated timestamp is required.", nameof(updatedAt));
    }

    UpdatedAt = updatedAt;
    UpdatedByUserId = updatedByUserId;
    RefreshConcurrencyToken();
  }
}
