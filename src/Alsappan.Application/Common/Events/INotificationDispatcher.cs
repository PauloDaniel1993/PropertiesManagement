using Alsappan.Domain.Common.Events;

namespace Alsappan.Application.Common.Events;

public interface INotificationDispatcher
{
  Task DispatchAsync(ModuleEventEnvelope envelope, CancellationToken cancellationToken = default);
}
