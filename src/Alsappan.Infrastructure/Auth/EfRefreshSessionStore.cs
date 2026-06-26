using Alsappan.Application.Common.Auth;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Auth;

public sealed class EfRefreshSessionStore : IRefreshSessionStore
{
  private readonly AlsappanDbContext dbContext;

  public EfRefreshSessionStore(AlsappanDbContext dbContext)
  {
    this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public async ValueTask CreateAsync(
    RefreshSessionRecord session,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(session);

    dbContext.RefreshSessions.Add(RefreshSession.FromRecord(session));
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async ValueTask<RefreshSessionRecord?> FindByIdAsync(
    EntityId sessionId,
    CancellationToken cancellationToken = default)
  {
    var session = await dbContext.RefreshSessions
      .AsNoTracking()
      .SingleOrDefaultAsync(candidate => candidate.Id == sessionId, cancellationToken)
      .ConfigureAwait(false);

    return session?.ToRecord();
  }

  public async ValueTask<RefreshSessionRecord?> FindByTokenHashAsync(
    string tokenHash,
    CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

    var normalizedHash = tokenHash.Trim();
    var session = await dbContext.RefreshSessions
      .AsNoTracking()
      .SingleOrDefaultAsync(candidate => candidate.TokenHash == normalizedHash, cancellationToken)
      .ConfigureAwait(false);

    return session?.ToRecord();
  }

  public async ValueTask RevokeAsync(
    EntityId sessionId,
    DateTimeOffset revokedAt,
    string? reason = null,
    CancellationToken cancellationToken = default)
  {
    var session = await dbContext.RefreshSessions
      .SingleOrDefaultAsync(candidate => candidate.Id == sessionId, cancellationToken)
      .ConfigureAwait(false);

    if (session is null)
    {
      return;
    }

    session.Revoke(revokedAt, reason);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async ValueTask RotateAsync(
    EntityId currentSessionId,
    RefreshSessionRecord replacementSession,
    DateTimeOffset revokedAt,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(replacementSession);

    var currentSession = await dbContext.RefreshSessions
      .SingleOrDefaultAsync(candidate => candidate.Id == currentSessionId, cancellationToken)
      .ConfigureAwait(false);

    if (currentSession is not null)
    {
      currentSession.Revoke(revokedAt, "rotated", replacementSession.Id);
    }

    dbContext.RefreshSessions.Add(RefreshSession.FromRecord(replacementSession));
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }
}
