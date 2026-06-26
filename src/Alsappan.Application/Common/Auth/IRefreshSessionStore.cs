using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Common.Auth;

public interface IRefreshSessionStore
{
  ValueTask CreateAsync(RefreshSessionRecord session, CancellationToken cancellationToken = default);

  ValueTask<RefreshSessionRecord?> FindByIdAsync(
    EntityId sessionId,
    CancellationToken cancellationToken = default);

  ValueTask<RefreshSessionRecord?> FindByTokenHashAsync(
    string tokenHash,
    CancellationToken cancellationToken = default);

  ValueTask RevokeAsync(
    EntityId sessionId,
    DateTimeOffset revokedAt,
    string? reason = null,
    CancellationToken cancellationToken = default);

  ValueTask RotateAsync(
    EntityId currentSessionId,
    RefreshSessionRecord replacementSession,
    DateTimeOffset revokedAt,
    CancellationToken cancellationToken = default);
}
