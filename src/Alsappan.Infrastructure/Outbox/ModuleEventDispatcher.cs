using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Events;
using Alsappan.Domain.Common.Events;

namespace Alsappan.Infrastructure.Outbox;

public sealed class ModuleEventDispatcher : IModuleEventDispatcher
{
  private readonly IAuditWriter _auditWriter;
  private readonly ITimelineProjectionWriter _timelineProjectionWriter;
  private readonly INotificationDispatcher _notificationDispatcher;

  public ModuleEventDispatcher(
    IAuditWriter auditWriter,
    ITimelineProjectionWriter timelineProjectionWriter,
    INotificationDispatcher notificationDispatcher)
  {
    _auditWriter = auditWriter ?? throw new ArgumentNullException(nameof(auditWriter));
    _timelineProjectionWriter = timelineProjectionWriter ??
      throw new ArgumentNullException(nameof(timelineProjectionWriter));
    _notificationDispatcher = notificationDispatcher ??
      throw new ArgumentNullException(nameof(notificationDispatcher));
  }

  public async Task DispatchAsync(
    ModuleEventEnvelope envelope,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(envelope);

    if (envelope.Consumers.HasFlag(ModuleEventConsumer.Audit))
    {
      await _auditWriter
        .WriteAsync(AuditEntryDraft.FromModuleEvent(envelope), cancellationToken)
        .ConfigureAwait(false);
    }

    if (envelope.Consumers.HasFlag(ModuleEventConsumer.Timeline))
    {
      await _timelineProjectionWriter
        .ProjectAsync(envelope, cancellationToken)
        .ConfigureAwait(false);
    }

    if (envelope.Consumers.HasFlag(ModuleEventConsumer.Notifications))
    {
      await _notificationDispatcher
        .DispatchAsync(envelope, cancellationToken)
        .ConfigureAwait(false);
    }
  }
}
