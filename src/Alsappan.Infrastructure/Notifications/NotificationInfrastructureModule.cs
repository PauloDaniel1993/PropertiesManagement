using Alsappan.Application.Notifications;
using Alsappan.Application.Notifications.Repositories;
using Alsappan.Infrastructure.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Notifications;

#pragma warning disable CA1812
internal sealed class NotificationInfrastructureModule : IInfrastructureModule
{
  public int Order => 320;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<INotificationRepository, EfNotificationRepository>();
    services.AddScoped<INotificationService, NotificationService>();
  }
}
