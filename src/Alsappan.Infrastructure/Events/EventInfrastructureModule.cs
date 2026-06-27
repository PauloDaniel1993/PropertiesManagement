using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Events;
using Alsappan.Infrastructure.Audit;
using Alsappan.Infrastructure.Modules;
using Alsappan.Infrastructure.Notifications;
using Alsappan.Infrastructure.Outbox;
using Alsappan.Infrastructure.Timeline;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Events;

#pragma warning disable CA1812
internal sealed class EventInfrastructureModule : IInfrastructureModule
{
  public int Order => 800;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<IAuditWriter, EfAuditWriter>();
    services.AddScoped<IModuleEventOutboxWriter, EfModuleEventOutboxWriter>();
    services.AddScoped<ITimelineProjectionWriter, EfTimelineProjectionWriter>();
    services.AddScoped<INotificationDispatcher, EfNotificationDispatcher>();
    services.AddScoped<IModuleEventDispatcher, ModuleEventDispatcher>();
    services.AddScoped<OutboxProcessor>();
  }
}
