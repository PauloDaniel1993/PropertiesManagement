using Alsappan.Application.Common.Events;
using Alsappan.Domain.Common.Events;
using Alsappan.Infrastructure.Persistence;

namespace Alsappan.Infrastructure.Notifications;

public sealed class EfNotificationDispatcher : INotificationDispatcher
{
  private readonly AlsappanDbContext _dbContext;
  private readonly TimeProvider _clock;

  public EfNotificationDispatcher(AlsappanDbContext dbContext, TimeProvider? clock = null)
  {
    _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    _clock = clock ?? TimeProvider.System;
  }

  public async Task DispatchAsync(
    ModuleEventEnvelope envelope,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(envelope);

    if (!envelope.Consumers.HasFlag(ModuleEventConsumer.Notifications))
    {
      return;
    }

    _dbContext.NotificationRecords.Add(
      NotificationRecord.FromEnvelope(envelope, _clock.GetUtcNow()));
    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }
}
