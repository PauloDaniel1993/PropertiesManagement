using Alsappan.Application.Inspections;
using Alsappan.Application.Inspections.Repositories;
using Alsappan.Infrastructure.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Inspections;

#pragma warning disable CA1812
internal sealed class InspectionInfrastructureModule : IInfrastructureModule
{
  public int Order => 390;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<IInspectionRepository, EfInspectionRepository>();
    services.AddScoped<IInspectionService, InspectionService>();
  }
}
