namespace Alsappan.Application.Common.Auth;

public interface IAuthenticatedUserProvider
{
  ValueTask<AuthenticatedUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
