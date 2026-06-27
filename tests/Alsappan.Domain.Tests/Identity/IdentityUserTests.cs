using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Identity;

namespace Alsappan.Domain.Tests.Identity;

public sealed class IdentityUserTests
{
  [Fact]
  public void PasswordPolicyRequiresStrongPassword()
  {
    var result = PasswordPolicy.Default.Validate("weak");

    Assert.False(result.IsValid);
    Assert.Contains(PasswordPolicyFailureCodes.TooShort, result.FailureCodes);
    Assert.Contains(PasswordPolicyFailureCodes.RequiresUppercase, result.FailureCodes);
    Assert.Contains(PasswordPolicyFailureCodes.RequiresDigit, result.FailureCodes);
    Assert.Contains(PasswordPolicyFailureCodes.RequiresNonAlphanumeric, result.FailureCodes);
  }

  [Fact]
  public void UserLocksAfterConfiguredFailedLoginThreshold()
  {
    var now = DateTimeOffset.Parse("2026-06-27T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
    var user = IdentityUser.Create(
      UserId.New(),
      "admin@example.com",
      "Admin",
      UserAccountType.Admin,
      now,
      status: UserStatus.Active);
    user.SetPasswordHash("hash", now, null);

    user.RecordFailedLogin(now.AddMinutes(1), maxFailedAccessAttempts: 2, TimeSpan.FromMinutes(10));
    user.RecordFailedLogin(now.AddMinutes(2), maxFailedAccessAttempts: 2, TimeSpan.FromMinutes(10));

    Assert.Equal(UserStatus.Locked, user.Status);
    Assert.True(user.IsLockoutActive(now.AddMinutes(3)));
    Assert.False(user.CanAttemptPasswordLogin(now.AddMinutes(3)));
  }

  [Fact]
  public void SuccessfulLoginClearsFailedAttemptState()
  {
    var now = DateTimeOffset.Parse("2026-06-27T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
    var user = IdentityUser.Create(
      UserId.New(),
      "admin@example.com",
      "Admin",
      UserAccountType.Admin,
      now,
      status: UserStatus.Active);
    user.SetPasswordHash("hash", now, null);
    user.RecordFailedLogin(now.AddMinutes(1));

    user.RecordSuccessfulLogin(now.AddMinutes(2));

    Assert.Equal(UserStatus.Active, user.Status);
    Assert.Equal(0, user.FailedLoginCount);
    Assert.Null(user.LockedUntil);
    Assert.Equal(now.AddMinutes(2), user.LastLoginAt);
  }
}
