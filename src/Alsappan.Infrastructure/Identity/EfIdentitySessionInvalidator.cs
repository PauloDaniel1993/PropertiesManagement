using Alsappan.Application.Identity.Sessions;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Infrastructure.Auth;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Identity;

public sealed class EfIdentitySessionInvalidator : IIdentitySessionInvalidator
{
  private readonly AlsappanDbContext dbContext;

  public EfIdentitySessionInvalidator(AlsappanDbContext dbContext)
  {
    this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public async Task RevokeUserSessionsAsync(
    UserId userId,
    DateTimeOffset revokedAt,
    string reason,
    CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(reason);

    var sessions = await dbContext.Set<RefreshSession>()
      .Where(session => session.UserId == userId && session.RevokedAt == null)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    foreach (var session in sessions)
    {
      session.Revoke(revokedAt, reason);
    }

    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }
}
