using Alsappan.Application.Timeline;
using Alsappan.Application.Timeline.Repositories;
using Alsappan.Infrastructure.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Timeline;

#pragma warning disable CA1812
internal sealed class TimelineInfrastructureModule : IInfrastructureModule
{
  public int Order => 500;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<ITimelineRepository, EfTimelineRepository>();
    services.AddScoped<ITimelineService, TimelineService>();
  }
}
