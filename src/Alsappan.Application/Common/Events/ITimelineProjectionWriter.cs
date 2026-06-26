using Alsappan.Domain.Common.Events;

namespace Alsappan.Application.Common.Events;

public interface ITimelineProjectionWriter
{
  Task ProjectAsync(ModuleEventEnvelope envelope, CancellationToken cancellationToken = default);
}
