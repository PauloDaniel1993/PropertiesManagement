using Alsappan.Application.Common.Events;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Outbox;

public sealed class OutboxProcessor
{
  private readonly AlsappanDbContext _dbContext;
  private readonly IModuleEventDispatcher _dispatcher;
  private readonly TimeProvider _clock;

  public OutboxProcessor(
    AlsappanDbContext dbContext,
    IModuleEventDispatcher dispatcher,
    TimeProvider? clock = null)
  {
    _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    _clock = clock ?? TimeProvider.System;
  }

  public async Task<OutboxProcessingResult> ProcessPendingAsync(
    int batchSize = 25,
    CancellationToken cancellationToken = default)
  {
    if (batchSize <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");
    }

    var messages = await _dbContext.OutboxMessages
      .IgnoreQueryFilters()
      .Where(message => message.ProcessedAt == null)
      .OrderBy(message => message.EnqueuedAt)
      .ThenBy(message => message.Id)
      .Take(batchSize)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    var processed = 0;
    var failed = 0;

    foreach (var message in messages)
    {
      cancellationToken.ThrowIfCancellationRequested();

      try
      {
        await _dispatcher
          .DispatchAsync(message.ToEnvelope(), cancellationToken)
          .ConfigureAwait(false);

        message.MarkProcessed(_clock.GetUtcNow());
        processed++;
      }
      catch (Exception exception) when (exception is not OperationCanceledException)
      {
        message.MarkFailed(exception.Message, _clock.GetUtcNow());
        failed++;
      }
    }

    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    var pending = await _dbContext.OutboxMessages
      .IgnoreQueryFilters()
      .CountAsync(message => message.ProcessedAt == null, cancellationToken)
      .ConfigureAwait(false);

    return new OutboxProcessingResult(processed, failed, pending);
  }
}
