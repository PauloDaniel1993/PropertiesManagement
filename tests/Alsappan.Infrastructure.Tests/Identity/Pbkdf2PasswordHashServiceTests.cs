using Alsappan.Application.Identity.Security;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Identity;
using Alsappan.Infrastructure.Identity;

namespace Alsappan.Infrastructure.Tests.Identity;

public sealed class Pbkdf2PasswordHashServiceTests
{
  [Fact]
  public void HashPasswordCreatesVerifiableNonPlaintextHash()
  {
    var user = IdentityUser.Create(
      UserId.New(),
      "admin@example.com",
      "Admin",
      UserAccountType.Admin,
      DateTimeOffset.UtcNow,
      status: UserStatus.Active);
    var service = new Pbkdf2PasswordHashService();

    var hash = service.HashPassword(user, "StrongPass123!");

    Assert.NotEqual("StrongPass123!", hash);
    Assert.Equal(
      PasswordVerificationResult.Success,
      service.VerifyPassword(user, "StrongPass123!", hash));
    Assert.Equal(
      PasswordVerificationResult.Failed,
      service.VerifyPassword(user, "WrongPass123!", hash));
  }
}
