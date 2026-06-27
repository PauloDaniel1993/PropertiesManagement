using Alsappan.Application.Dashboard;
using Alsappan.Application.Dashboard.Repositories;
using Alsappan.Infrastructure.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Dashboard;

#pragma warning disable CA1812
internal sealed class DashboardInfrastructureModule : IInfrastructureModule
{
  public int Order => 410;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<IDashboardRepository, EfDashboardRepository>();
    services.AddScoped<IDashboardService, DashboardService>();
  }
}
