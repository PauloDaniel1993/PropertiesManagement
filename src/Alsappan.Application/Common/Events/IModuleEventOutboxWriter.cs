using Alsappan.Domain.Common.Events;

namespace Alsappan.Application.Common.Events;

public interface IModuleEventOutboxWriter
{
  Task EnqueueAsync(ModuleEventEnvelope envelope, CancellationToken cancellationToken = default);
}
