using Alsappan.Application.Common.Events;
using Alsappan.Domain.Common.Events;
using Alsappan.Infrastructure.Persistence;

namespace Alsappan.Infrastructure.Timeline;

public sealed class EfTimelineProjectionWriter : ITimelineProjectionWriter
{
  private readonly AlsappanDbContext _dbContext;
  private readonly TimeProvider _clock;

  public EfTimelineProjectionWriter(AlsappanDbContext dbContext, TimeProvider? clock = null)
  {
    _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    _clock = clock ?? TimeProvider.System;
  }

  public async Task ProjectAsync(
    ModuleEventEnvelope envelope,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(envelope);

    if (!envelope.Consumers.HasFlag(ModuleEventConsumer.Timeline))
    {
      return;
    }

    _dbContext.TimelineEntries.Add(TimelineEntry.FromEnvelope(envelope, _clock.GetUtcNow()));
    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }
}
