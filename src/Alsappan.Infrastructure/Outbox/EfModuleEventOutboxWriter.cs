using Alsappan.Application.Common.Events;
using Alsappan.Domain.Common.Events;
using Alsappan.Infrastructure.Persistence;

namespace Alsappan.Infrastructure.Outbox;

public sealed class EfModuleEventOutboxWriter : IModuleEventOutboxWriter
{
  private readonly AlsappanDbContext _dbContext;
  private readonly TimeProvider _clock;

  public EfModuleEventOutboxWriter(AlsappanDbContext dbContext, TimeProvider? clock = null)
  {
    _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    _clock = clock ?? TimeProvider.System;
  }

  public async Task EnqueueAsync(
    ModuleEventEnvelope envelope,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(envelope);

    _dbContext.OutboxMessages.Add(
      OutboxMessage.FromEnvelope(envelope, _clock.GetUtcNow()));
    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }
}
