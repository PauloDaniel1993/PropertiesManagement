using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Identity.Sessions;

public interface IIdentitySessionInvalidator
{
  Task RevokeUserSessionsAsync(
    UserId userId,
    DateTimeOffset revokedAt,
    string reason,
    CancellationToken cancellationToken = default);
}
