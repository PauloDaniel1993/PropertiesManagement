using Alsappan.Domain.Common.Events;

namespace Alsappan.Application.Common.Events;

public interface IModuleEventDispatcher
{
  Task DispatchAsync(ModuleEventEnvelope envelope, CancellationToken cancellationToken = default);
}
