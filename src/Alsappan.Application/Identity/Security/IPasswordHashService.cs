using Alsappan.Domain.Identity;

namespace Alsappan.Application.Identity.Security;

public enum PasswordVerificationResult
{
  Failed,
  Success,
  SuccessRehashNeeded
}

public interface IPasswordHashService
{
  string HashPassword(IdentityUser user, string password);

  PasswordVerificationResult VerifyPassword(IdentityUser user, string password, string passwordHash);
}
